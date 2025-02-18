using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.Text.Json;

namespace KoiGuardian.Api.Services;

public interface IOrderService
{
    Task<List<OrderFilterResponse>> FilterOrder(OrderFilterRequest request);
    Task<OrderDetailResponse> GetDetail(Guid orderId);
    Task<List<OrderResponse>> CreateOrderAsync(CreateOrderRequest request);
}

public class OrderService(
    IRepository<Order> orderRepository,
    IRepository<OrderDetail> orderDetailRepository,
    IRepository<Product> productRepository,
    IRepository<Member> memRepository,
    IUnitOfWork<KoiGuardianDbContext> uow,
    GhnService ghnService
    ) : IOrderService
{
    public async Task<List<OrderResponse>> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            if (request == null || request.OrderDetails == null || !request.OrderDetails.Any())
            {
                return new List<OrderResponse> { OrderResponse.Error("Invalid request data") };
            }

            string addressNote = JsonSerializer.Serialize(new
            {
                ProvinceName = request.Address.ProvinceName,
                ProvinceId = request.Address.ProvinceId,
                DistrictName = request.Address.DistrictName,
                DistrictId = request.Address.DistrictId,
                WardName = request.Address.WardName,
                WardId = request.Address.WardId
            });

            // ✅ Lấy danh sách Product trước (tránh gọi DB nhiều lần)
            var productIds = request.OrderDetails.Select(d => d.ProductId).ToList();
            var products = await productRepository.FindAsync(x => productIds.Contains(x.ProductId));

            // ✅ Nhóm sản phẩm theo ShopId
            var groupedOrderDetails = request.OrderDetails
                .GroupBy(d => products.FirstOrDefault(p => p.ProductId == d.ProductId)?.ShopId ?? Guid.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<OrderResponse> responses = new List<OrderResponse>();

            foreach (var group in groupedOrderDetails)
            {
                Guid shopId = group.Key;
                var orderDetails = group.Value;

                // Tính tổng giá trị đơn hàng cho từng ShopId
                decimal total = 0;
                foreach (var detail in orderDetails)
                {
                    var product = products.FirstOrDefault(p => p.ProductId == detail.ProductId);
                    if (product != null)
                    {
                        total += product.Price * detail.Quantity; // Tính tổng giá trị sản phẩm
                    }
                }

                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    ShopId = shopId,
                    UserId = request.AccountId,
                    ShipType = request.ShipType,
                    oder_code = $"ORD-{DateTime.UtcNow.Ticks}", // Sinh mã đơn hàng
                    Status = request.Status,
                    ShipFee = request.ShipFee.ToString("C"), // Định dạng tiền tệ
                    Total = (float)total, // Gán tổng giá trị
                    Note = addressNote,
                    OrderDetail = new List<OrderDetail>()
                };

                orderRepository.Insert(order);

                foreach (var detail in orderDetails)
                {
                    var orderDetail = new OrderDetail
                    {
                        OderDetailId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity
                    };

                    orderDetailRepository.Insert(orderDetail);
                }

                responses.Add(OrderResponse.Success($"Order created successfully with ID: {order.OrderId}"));
            }

            await uow.SaveChangesAsync();

            return responses;
        }
        catch (Exception ex)
        {
            return new List<OrderResponse> { OrderResponse.Error($"Failed to create order: {ex.Message}") };
        }
    }
    public async Task<List<OrderFilterResponse>> FilterOrder(OrderFilterRequest request)
    {
        var result = new List<Order>();
        if ( request.AccountId != null)
        {
            result = (await orderRepository.FindAsync( u => u.UserId == request.AccountId, 
                include : u=> u.Include( u => u.Shop).Include(u=> u.User)
                , CancellationToken.None)).ToList();
        }

        var processingOrder = result.Where( u=> 
            u.Status != OrderStatus.Complete.ToString() &&
            u.Status != OrderStatus.Pending.ToString() &&
            u.Status != OrderStatus.Confirm.ToString()
        );
        if (processingOrder.Any())
        {
            JsonDocument doc = null ;
            foreach (var order in processingOrder)
            {
                var tracking = new TrackingGHNRequest()
                {
                    order_code = order.oder_code
                };

                doc = JsonDocument.Parse(await ghnService.TrackingShippingOrder(tracking));

                string? status = doc.RootElement
                                    .GetProperty("data")
                                    .GetProperty("status")
                                    .GetString();

                order.Status = status ?? OrderStatus.Inprogress.ToString();

                orderRepository.Update(order);
            }
            await uow.SaveChangesAsync();
        }
        if (string.IsNullOrEmpty(request.RequestStatus))
        {
            result = result.Where( u => u.Status.ToLower() == request.RequestStatus.ToLower()).ToList();
        }

        if (string.IsNullOrEmpty(request.SearchKey))
        {
            result = result.Where(u => u.Note.ToLower().Contains(request.RequestStatus.ToLower())).ToList();
        }

        var mem = await memRepository.FindAsync( u => result.Select( u => u.UserId ).Contains(u.UserId), CancellationToken.None);

        return result.Select( u => new OrderFilterResponse()
        {
            OrderId = u.OrderId,
            ShopName = u.Shop.ShopName,
            CustomerName = u.User.UserName,
            CustomerAddress = mem.FirstOrDefault( a => a.UserId ==  u.UserId).Address,
            CustomerPhoneNumber = u.User.PhoneNumber,
            ShipFee = u.ShipFee,
            oder_code = u.oder_code,
            Status = u.Status,
            ShipType = u.ShipType,
            Note = u.Note
        }).ToList();
    }

    public async Task<OrderDetailResponse> GetDetail(Guid orderId)
    {
        var order = await orderRepository.GetAsync(u => u.OrderId == orderId,
      include: u => u.Include(u => u.OrderDetail)
                    .Include(u => u.Shop)
                    .Include(u => u.User));

        if (order == null) return new();
        var mem = await memRepository.GetAsync(u => u.UserId == order.UserId, CancellationToken.None);

        return new OrderDetailResponse() {
            OrderId = order.OrderId,
            ShopName = order.Shop.ShopName,
            CustomerName = order.User.UserName,
            CustomerAddress = JsonSerializer.Deserialize<AddressDto>(order.Note)?.ToString() ?? "No address info",
            CustomerPhoneNumber = order.User.PhoneNumber,
            ShipFee = order.ShipFee,
            oder_code = order.oder_code,
            Status = order.Status,
            ShipType = order.ShipType,
            Note = order.Note,
            Details = order.OrderDetail
        };
    }
}
