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
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
}

public class OrderService(
    IRepository<Order> orderRepository,
    IRepository<OrderDetail> orderDetailRepository,
    IRepository<Member> memRepository,
    IUnitOfWork<KoiGuardianDbContext> uow,
    GhnService ghnService
    ) : IOrderService
{
    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            if (request == null || request.OrderDetails == null || !request.OrderDetails.Any())
            {
                return OrderResponse.Error("Invalid request data");
            }

            // Chuyển địa chỉ thành chuỗi Note
            string addressNote = $"{request.Address.ProvinceName},{request.Address.ProvinceId}, " +
                $"{request.Address.DistrictName},{request.Address.DistrictId}, " +
                $"{request.Address.WardName}, {request.Address.WardId}";

            // Tạo Order mới
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                ShopId = request.ShopId,
                AccountId = request.AccountId,
                ShipType = request.ShipType,
                oder_code = $"ORD-{DateTime.UtcNow.Ticks}", // Sinh mã đơn hàng
                Status = request.Status,
                ShipFee = request.ShipFee.ToString("C"), // Định dạng tiền tệ
                Note = addressNote,
                OrderDetail = new List<OrderDetail>()
            };

            orderRepository.Insert(order);

            // Thêm chi tiết đơn hàng
           /* foreach (var detail in request.OrderDetails)
            {
                var orderDetail = new OrderDetail
                {
                    OderDetailId = Guid.NewGuid(),
                    OderId = order.OrderId,
                    ProductId = detail.ProductId,
                };

                orderDetailRepository.Insert(orderDetail); // Sửa lỗi thiếu tham số
            }*/

            // Lưu thay đổi vào database
            await uow.SaveChangesAsync();

            return OrderResponse.Success($"Order created successfully with ID: {order.OrderId}");
        }
        catch (Exception ex)
        {
            return OrderResponse.Error($"Failed to create order: {ex.Message}");
        }


    }

    public async Task<List<OrderFilterResponse>> FilterOrder(OrderFilterRequest request)
    {
        var result = new List<Order>();
        if ( request.AccountId != null)
        {
            result = (await orderRepository.FindAsync( u => u.AccountId == request.AccountId, 
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

        var mem = await memRepository.FindAsync( u => result.Select( u => u.AccountId ).Contains(u.UserId), CancellationToken.None);

        return result.Select( u => new OrderFilterResponse()
        {
            OrderId = u.OrderId,
            ShopName = u.Shop.ShopName,
            CustomerName = u.User.UserName,
            CustomerAddress = mem.FirstOrDefault( a => a.UserId ==  u.AccountId).Address,
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
            include : u=> u.Include(u => u.OrderDetail));

        if (order == null) return new();
        var mem = await memRepository.GetAsync(u => u.UserId == order.AccountId, CancellationToken.None);

        return new OrderDetailResponse() {
            OrderId = order.OrderId,
            ShopName = order.Shop.ShopName,
            CustomerName = order.User.UserName,
            CustomerAddress = mem.Address,
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
