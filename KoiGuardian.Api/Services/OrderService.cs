using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
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
    Task<OrderResponse> UpdateOrderAsync(UpdateOrderRequest request);
    Task<OrderResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request);
    Task<OrderResponse> UpdateOrderCodeShipFeeAsync(UpdateOrderCodeShipFeeRequest request);
  /*  Task<OrderResponse> UpdateOrderShipFeeAsync(UpdateOrderShipFeeRequest request);*/
    Task<OrderResponse> UpdateOrderShipTypeAsync(UpdateOrderShipTypeRequest request);
    Task<List<OrderDetailResponse>> GetOrdersByShopIdAsync(Guid shopId);
}

public class OrderService(
    IRepository<Order> orderRepository,
    IRepository<Transaction> transactionRepository,
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
                    Address = addressNote,
                    Note = request.Note,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.MaxValue,
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

                responses.Add(OrderResponse.Success(order.OrderId.ToString()));
            }

            await uow.SaveChangesAsync();

            return responses;
        }
        catch (Exception ex)
        {
            return new List<OrderResponse> { OrderResponse.Error($"Failed to create order: {ex.Message}") };
        }
    }

    public async Task<OrderResponse> UpdateOrderAsync(UpdateOrderRequest request)
    {
        try
        {
            // Validate request
            if (request == null || request.OrderDetails == null || !request.OrderDetails.Any())
            {
                return OrderResponse.Error("Invalid request data");
            }

            // Find the existing order
            var order = await orderRepository.GetAsync(o => o.OrderId == request.OrderId,CancellationToken.None);
            if (order == null)
            {
                return OrderResponse.Error("Order not found");
            }

            // Update address note (JSON string)
            order.Address = JsonSerializer.Serialize(new
            {
                ProvinceName = request.Address.ProvinceName,
                ProvinceId = request.Address.ProvinceId,
                DistrictName = request.Address.DistrictName,
                DistrictId = request.Address.DistrictId,
                WardName = request.Address.WardName,
                WardId = request.Address.WardId
            });

            // Update order properties
            order.ShipType = request.ShipType;
            order.Status = request.Status;
            order.ShipFee = request.ShipFee.ToString("C");

            // Calculate total price
            var productIds = request.OrderDetails.Select(d => d.ProductId).ToList();
            var products = await productRepository.FindAsync(x => productIds.Contains(x.ProductId));

            decimal total = 0;
            foreach (var detail in request.OrderDetails)
            {
                var product = products.FirstOrDefault(p => p.ProductId == detail.ProductId);
                if (product != null)
                {
                    total += product.Price * detail.Quantity;
                }
            }
            order.Total = (float)total;

            orderRepository.Update(order);

            // Update order details
            var existingOrderDetails = await orderDetailRepository.FindAsync(od => od.OrderId == request.OrderId);

            // Delete removed order details
            var detailsToDelete = existingOrderDetails.Where(od => !request.OrderDetails.Any(d => d.ProductId == od.ProductId)).ToList();
            foreach (var detail in detailsToDelete)
            {
                orderDetailRepository.Delete(detail);
            }

            // Add or update existing order details
            foreach (var detail in request.OrderDetails)
            {
                var existingDetail = existingOrderDetails.FirstOrDefault(od => od.ProductId == detail.ProductId);
                if (existingDetail != null)
                {
                    existingDetail.Quantity = detail.Quantity;
                    orderDetailRepository.Update(existingDetail);
                }
                else
                {
                    var newOrderDetail = new OrderDetail
                    {
                        OderDetailId = Guid.NewGuid(),
                        OrderId = request.OrderId,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity
                    };
                    orderDetailRepository.Insert(newOrderDetail);
                }
            }

            // Check if the order still has any OrderDetails
            var remainingOrderDetails = await orderDetailRepository.FindAsync(od => od.OrderId == request.OrderId);
            if (!remainingOrderDetails.Any())
            {
                orderRepository.Delete(order);
            }

            await uow.SaveChangesAsync();

            return OrderResponse.Success("Order updated successfully");
        }
        catch (Exception ex)
        {
            return OrderResponse.Error($"Failed to update order: {ex.Message}");
        }
    }


    public async Task<List<OrderFilterResponse>> FilterOrder(OrderFilterRequest request)
    {
        /* 

       if (processingOrder.Any())
       {
           foreach (var order in processingOrder)
           {
               var tracking = new TrackingGHNRequest()
               {
                   order_code = order.oder_code
               };

               var trackingResponse = await ghnService.TrackingShippingOrder(tracking);
               if (!string.IsNullOrEmpty(trackingResponse))
               {
                   var doc = JsonDocument.Parse(trackingResponse);
                   string? status = doc.RootElement
                       .GetProperty("data")
                       .GetProperty("status")
                       .GetString();

                   order.Status = status ?? OrderStatus.Inprogress.ToString();
                   orderRepository.Update(order);
               }
           }
           await uow.SaveChangesAsync();
       }*/
        var result = new List<Order>();

        if (request.AccountId != null)
        {
            result = (await orderRepository.FindAsync(u => u.UserId == request.AccountId,
                include: u => u.Include(u => u.Shop).Include(u => u.User).Include(u => u.OrderDetail),
                CancellationToken.None)).ToList();
        }

        // Truy vấn danh sách Transaction có DocNo trùng với OrderId
        var transactionList = await transactionRepository.FindAsync(
            t => result.Select(o => o.OrderId).Contains(t.DocNo),
            CancellationToken.None
        );

        var mem = await memRepository.FindAsync(u => result.Select(u => u.UserId).Contains(u.UserId), CancellationToken.None);

        return result.Select(u =>
        {
            var address = !string.IsNullOrEmpty(u.Address)
                ? JsonSerializer.Deserialize<AddressDto>(u.Address)
                : new AddressDto { ProvinceName = "No address info" };

            var transaction = transactionList.FirstOrDefault(t => t.DocNo == u.OrderId);

            return new OrderFilterResponse()
            {
                OrderId = u.OrderId,
                ShopName = u.Shop.ShopName,
                CustomerName = u.User.UserName,
                CustomerAddress = address?.ToString(),
                CustomerPhoneNumber = u.User.PhoneNumber,
                ShipFee = u.ShipFee,
                oder_code = u.oder_code,
                Status = u.Status,
                ShipType = u.ShipType,
                Note = u.Note,

                // Object chứa thông tin transaction
                TransactionInfo = transaction != null ? new TransactionDto
                {
                    TransactionId = transaction.TransactionId,
                    TransactionDate = transaction.TransactionDate,
                    TransactionType = transaction.TransactionType,
                    VnPayTransactionId = transaction.VnPayTransactionid
                } : null,
                Details = u.OrderDetail.Select(d => new OrderDetailDto
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity
                }).ToList()
            };
        }).ToList();
    }

    public async Task<OrderDetailResponse> GetDetail(Guid orderId)
    {
        var order = await orderRepository.GetAsync(u => u.OrderId == orderId,
            include: u => u.Include(u => u.OrderDetail)
                            .Include(u => u.Shop)
                            .Include(u => u.User));

        if (order == null) return new();

        AddressDto addressDto;
        try
        {
            addressDto = !string.IsNullOrEmpty(order.Address)
                ? JsonSerializer.Deserialize<AddressDto>(order.Address)
                : new AddressDto { ProvinceName = "No address info" };
        }
        catch (JsonException)
        {
            addressDto = new AddressDto { ProvinceName = "Invalid address" };
        };

        return new OrderDetailResponse()
        {
            OrderId = order.OrderId,
            ShopName = order.Shop.ShopName,
            CustomerName = order.User.UserName,
            CustomerAddress = addressDto,
            CustomerPhoneNumber = order.User.PhoneNumber,
            ShipFee = order.ShipFee,
            oder_code = order.oder_code,
            Status = order.Status,
            ShipType = order.ShipType,
            Note = order.Note,
            Details = order.OrderDetail.Select(d => new OrderDetailDto
            {
                ProductId = d.ProductId,
                Quantity = d.Quantity
            }).ToList()
        };
    }

    public async Task<OrderResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await orderRepository.GetAsync(o => o.OrderId == request.OrderId, CancellationToken.None);
            var trasanction = await transactionRepository.FindAsync(o => o.DocNo.ToString().Contains(order.OrderId.ToString()),CancellationToken.None);
            if (order == null)
            {
                return OrderResponse.Error("Order not found");
            }

            if (OrderStatus.Complete.ToString().ToLower().Equals(request.Status) && trasanction.Count == 0)
            {
                order.UpdatedDate = DateTime.UtcNow;
                order.Status = request.Status;
                transactionRepository.Insert(new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    TransactionDate = DateTime.UtcNow,
                    TransactionType = TransactionType.Pending.ToString(),
                    VnPayTransactionid = "Order Paid (COD)",
                    UserId = order.UserId,
                    DocNo = order.OrderId,
                });

            }
            else
            {
                order.Status = request.Status;
            }
            orderRepository.Update(order);
            await uow.SaveChangesAsync();

            return OrderResponse.Success("Order updated successfully");
        }
        catch (Exception ex)
        {
            return OrderResponse.Error($"Failed to update order: {ex.Message}");
        }
    }

    public async Task<OrderResponse> UpdateOrderCodeShipFeeAsync(UpdateOrderCodeShipFeeRequest request)
    {
        try
        {
            var order = await orderRepository.GetAsync(o => o.OrderId == request.OrderId, CancellationToken.None);
            if (order == null)
            {
                return OrderResponse.Error("Order not found");
            }
            order.oder_code = request.order_code;
            order.ShipFee = request.ShipFee;
            orderRepository.Update(order);
            await uow.SaveChangesAsync();

            return OrderResponse.Success("Order updated successfully");
        }
        catch (Exception ex)
        {
            return OrderResponse.Error($"Failed to update order: {ex.Message}");
        }
        throw new NotImplementedException();
    }

    public async Task<List<OrderDetailResponse>> GetOrdersByShopIdAsync(Guid shopId)
    {
        var orders = await orderRepository.FindAsync(
            predicate: o => o.ShopId == shopId,
            include: o => o.Include(o => o.OrderDetail)
                           .Include(o => o.Shop)
                           .Include(o => o.User),
            orderBy: o => o.OrderByDescending(o => o.CreatedDate)
        );
        return orders.Select(order =>
        {
            AddressDto customerAddress;
            try
            {
                customerAddress = !string.IsNullOrEmpty(order.Address)
                    ? JsonSerializer.Deserialize<AddressDto>(order.Address)
                    : new AddressDto { ProvinceName = "No address info" };
            }
            catch (JsonException)
            {
                customerAddress = new AddressDto { ProvinceName = "Invalid address" };
            }

            return new OrderDetailResponse
            {
                OrderId = order.OrderId,
                ShopName = order.Shop?.ShopName ?? "Unknown Shop",
                CustomerName = order.User?.UserName ?? "Unknown Customer",
                CustomerAddress = customerAddress,
                CustomerPhoneNumber = order.User?.PhoneNumber ?? "No phone number",
                ShipFee = order.ShipFee,
                oder_code = order.oder_code,
                Status = order.Status,
                ShipType = order.ShipType,
                Note = order.Note,
                Details = order.OrderDetail?.Select(d => new OrderDetailDto
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity
                }).ToList() ?? new List<OrderDetailDto>() // Tránh lỗi null
            };
        }).ToList();
    }

/*    public async Task<OrderResponse> UpdateOrderShipFeeAsync(UpdateOrderShipFeeRequest request)
    {
        try
        {
            var order = await orderRepository.GetAsync(o => o.OrderId == request.OrderId, CancellationToken.None);
            if (order == null)
            {
                return OrderResponse.Error("Order not found");
            }
            order.ShipFee = request.ShipFee;
            orderRepository.Update(order);
            await uow.SaveChangesAsync();

            return OrderResponse.Success("Order updated successfully");
        }
        catch (Exception ex)
        {
            return OrderResponse.Error($"Failed to update order: {ex.Message}");
        }
        throw new NotImplementedException();
    }*/

    public async Task<OrderResponse> UpdateOrderShipTypeAsync(UpdateOrderShipTypeRequest request)
    {
        try
        {
            var order = await orderRepository.GetAsync(o => o.OrderId == request.OrderId, CancellationToken.None);
            if (order == null)
            {
                return OrderResponse.Error("Order not found");
            }
            order.ShipType = request.ShipType;
            orderRepository.Update(order);
            await uow.SaveChangesAsync();

            return OrderResponse.Success("Order updated successfully");
        }
        catch (Exception ex)
        {
            return OrderResponse.Error($"Failed to update order: {ex.Message}");
        }
        throw new NotImplementedException();
    }
}
