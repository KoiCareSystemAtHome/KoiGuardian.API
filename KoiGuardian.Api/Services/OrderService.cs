using KoiGuardian.Core.Repository;
using KoiGuardian.Core.UnitOfWork;
using KoiGuardian.DataAccess;
using KoiGuardian.DataAccess.Db;
using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Request;
using KoiGuardian.Models.Response;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;

namespace KoiGuardian.Api.Services;

public interface IOrderService
{
    Task<List<OrderFilterResponse>> FilterOrder(OrderFilterRequest request);
    Task<OrderDetailResponse> GetDetail(Guid orderId);
    Task<List<OrderResponse>> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse> UpdateOrderAsync(UpdateOrderRequest request);
    Task<OrderResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request);
    Task<OrderResponse> CancelOrderAsync(RejectOrderRequest request);
    Task<OrderResponse> UpdateOrderCodeShipFeeAsync(UpdateOrderCodeShipFeeRequest request);
  /*  Task<OrderResponse> UpdateOrderShipFeeAsync(UpdateOrderShipFeeRequest request);*/
    Task<OrderResponse> UpdateOrderShipTypeAsync(UpdateOrderShipTypeRequest request);
    Task<List<OrderDetailResponse>> GetOrdersByShopIdAsync(Guid shopId);
    Task<List<OrderFilterResponse>> GetAllOrders(CancellationToken cancellationToken = default);

    Task<List<InvoiceDetailResponse>> CalculateOrderInvoicesAsync(CreateOrderRequest request);
}

public class OrderService(
    IRepository<Order> orderRepository,
    IRepository<Transaction> transactionRepository,
    IRepository<OrderDetail> orderDetailRepository,
    IRepository<Wallet> walletRepository,
    IRepository<Product> productRepository,
    IRepository<Member> memRepository,
    IRepository<Shop> shopRepository,
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
                var shop = await shopRepository.GetAsync(x=> x.ShopId.Equals(shopId),CancellationToken.None);

                // Tính tổng giá trị đơn hàng cho từng ShopId
                decimal total = 0;
                int totalWeight = 0;
                foreach (var detail in orderDetails)
                {
                    var product = products.FirstOrDefault(p => p.ProductId == detail.ProductId);
                    if (product != null)
                    {
                        if (product.StockQuantity < detail.Quantity)
                        {
                            return new List<OrderResponse> { OrderResponse.Error("Số Lượng Hàng Không Hợp lệ") };
                        }
                        product.StockQuantity -=  detail.Quantity;
                        productRepository.Update(product);
                        total += product.Price * detail.Quantity; // Tính tổng giá trị sản phẩm
                        totalWeight += (int)product.Weight * detail.Quantity; 
                    } 
                    
                }

                int districtId;
                if (!int.TryParse(request.Address.DistrictId, out districtId))
                {
                    return new List<OrderResponse> { OrderResponse.Error("Invalid DistrictId format. Must be a valid integer.") };
                }

                var feeRequest = new GHNShippingFeeReuqest
                {
                    service_type_id = 2,
                    to_district_id = districtId,
                    to_ward_code = request.Address.WardId.ToString(),
                    weight = totalWeight,
                    length = 10, // Giả sử giá trị mặc định
                    width = 10,
                    height = 10,
                    insurance_value = 0,
                    coupon = "",
                    items = orderDetails.Select(d => new Item
                    {
                        name = products.FirstOrDefault(p => p.ProductId == d.ProductId)?.ProductName ?? "Product",
                        quantity = d.Quantity,
                        weight = (int)products.FirstOrDefault(p => p.ProductId == d.ProductId).Weight,
                    }).ToList()
                };



                var shippingFeeResponse = await ghnService.CalculateShippingFee(feeRequest, shop.GHNId);
                if (shippingFeeResponse == null)
                {
                    return new List<OrderResponse> { OrderResponse.Error("Không thể tính phí vận chuyển") };
                }
                decimal shippingFee = shippingFeeResponse.Data.Total;


                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    ShopId = shopId,
                    UserId = request.AccountId,
                    ShipType = request.ShipType,
                    oder_code = $"{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}", // Sinh mã đơn hàng
                    Status = request.Status,
                    ShipFee = shippingFee.ToString(), // Định dạng tiền tệ
                    Total = (float)total, // Gán tổng giá trị
                    Address = addressNote,
                    PhoneNumber = request.PhoneNumber,
                    Name = request.Name,
                    Note = request.Note,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.MaxValue,
                    OrderDetail = new List<OrderDetail>()
                };
                orderRepository.Insert(order);
                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    TransactionDate = DateTime.UtcNow,
                    TransactionType = TransactionType.Pending.ToString(),
                    VnPayTransactionid = "Order Paid",
                    UserId = request.AccountId,
                    DocNo = order.OrderId,
                };
                transactionRepository.Insert(transaction);
                

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
                include: u => u.Include(u => u.Shop).Include(u => u.User).Include(u => u.OrderDetail).Include(u => u.Report),
                CancellationToken.None)).ToList();
        }

        result = result.OrderByDescending(u => u.CreatedDate).ToList();

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
                    VnPayTransactionId = transaction.VnPayTransactionid,
                    Payment = !string.IsNullOrEmpty(transaction.Payment)
                ? JsonSerializer.Deserialize<PaymentInfo>(transaction.Payment)
                : null,

                    Refund = !string.IsNullOrEmpty(transaction.Refund)
                ? JsonSerializer.Deserialize<RefundInfo>(transaction.Refund)
                : null,

                } : null,

                ReportDetail = u.Report != null ? new ReportDetailResponse 
                {
                    ReportId = u.Report.ReportId,
                    CreatedDate = u.Report.CreatedDate,
                    image = u.Report.image,
                    status = u.Report.status,
                    Reason = u.Report.Reason,
                    OrderId = u.Report.OrderId,
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
            RecieverName = order.Name,
            RecieverPhone = order.PhoneNumber,
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
            if (order == null)
            {
                return OrderResponse.Error("Order not found");
            }

            if (order.Status.ToLower() == OrderStatus.Cancel.ToString().ToLower() ||
                order.Status.ToLower() == OrderStatus.Fail.ToString().ToLower())
            {
                return OrderResponse.Success("Order is already in Cancel or Fail status");
            }

            var transaction = await transactionRepository.GetAsync(o => o.DocNo.ToString().Contains(order.OrderId.ToString()), CancellationToken.None);
            string status = request.Status.ToLower();

            if (status == OrderStatus.Complete.ToString().ToLower() && transaction.Payment == null)
            {
                //order.UpdatedDate = DateTime.UtcNow;
                order.Status = request.Status;
                var paymentInfo = new PaymentInfo
                {
                    Amount = (decimal)(order.Total + long.Parse(order.ShipFee)),
                    Date = DateTime.UtcNow,
                    PaymentMethod = "COD",
                    Description = $"Thanh toán cho hóa đơn {order.OrderId}"
                };
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,  // Tạo định dạng xuống dòng và thụt đầu dòng
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // Hỗ trợ ký tự tiếng Việt
                };
                string paymentJson = JsonSerializer.Serialize(paymentInfo, jsonOptions);
                transaction.Payment = paymentJson;
                transactionRepository.Update(transaction);
                /* transactionRepository.Insert(new Transaction
                 {
                     TransactionId = Guid.NewGuid(),
                     TransactionDate = DateTime.UtcNow,
                     Amount = order.Total,
                     TransactionType = TransactionType.Pending.ToString(),
                     VnPayTransactionid = "Order Paid (COD)",
                     UserId = order.UserId,
                     DocNo = order.OrderId,
                 });*/
            }
            else if (status == OrderStatus.Fail.ToString().ToLower() && transaction.Refund == null && transaction.Payment !=null
                     && transaction.TransactionType.ToLower() != TransactionType.Cancel.ToString().ToLower())
            {
                //order.UpdatedDate = DateTime.UtcNow;
                order.Status = request.Status;
                transaction.TransactionType = TransactionType.Cancel.ToString();

                var RefundInfo = new RefundInfo
                {
                    Amount = (decimal)order.Total,
                    Date = DateTime.UtcNow,
                    Description = $"Hoàn Tiền cho hóa đơn giao hàng thất bại {order.OrderId} "
                };
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,  // Tạo định dạng xuống dòng và thụt đầu dòng
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // Hỗ trợ ký tự tiếng Việt
                };
                string refundJson = JsonSerializer.Serialize(RefundInfo, jsonOptions);
                transaction.Refund = refundJson;
                transactionRepository.Update(transaction);

                var wallet = await walletRepository.GetAsync(x => x.UserId.Equals(order.UserId), CancellationToken.None);
                if (wallet != null)
                {
                    wallet.Amount += order.Total;
                    walletRepository.Update(wallet);
                }
            }

            else if (status == OrderStatus.Return.ToString().ToLower() && transaction.Refund == null && transaction.Payment != null
                    && transaction.TransactionType.ToLower() != TransactionType.Cancel.ToString().ToLower())
            {
                //order.UpdatedDate = DateTime.UtcNow;
                order.Status = request.Status;
                transaction.TransactionType = TransactionType.Cancel.ToString();

                var RefundInfo = new RefundInfo
                {
                    Amount = (decimal)order.Total,
                    Date = DateTime.UtcNow,
                    Description = $"Hoàn Tiền cho hóa đơn bị hoàn trả {order.OrderId}"
                };
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,  // Tạo định dạng xuống dòng và thụt đầu dòng
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // Hỗ trợ ký tự tiếng Việt
                };
                string refundJson = JsonSerializer.Serialize(RefundInfo, jsonOptions);
                transaction.Refund = refundJson;
                transactionRepository.Update(transaction);

                var wallet = await walletRepository.GetAsync(x => x.UserId.Equals(order.UserId), CancellationToken.None);
                if (wallet != null)
                {
                    wallet.Amount += order.Total;
                    walletRepository.Update(wallet);
                }
            }

            else if (status == OrderStatus.Return.ToString().ToLower() && transaction.Payment == null
                   && transaction.TransactionType.ToLower() != TransactionType.Cancel.ToString().ToLower())
            {
                //order.UpdatedDate = DateTime.UtcNow;
                order.Status = request.Status;
                transaction.TransactionType = TransactionType.Cancel.ToString();

                transactionRepository.Update(transaction);

               
            }

            else
            {
                order.Status = request.Status;
            }
            order.UpdatedDate = DateTime.UtcNow;
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
            //order.ShipFee = request.ShipFee;
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
                RecieverName = order.Name,
                RecieverPhone = order.PhoneNumber,
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

    public async Task<OrderResponse> CancelOrderAsync(RejectOrderRequest request)
    {
        try
        {
            // Lấy thông tin đơn hàng
            var order = await orderRepository.GetAsync(o => o.OrderId == request.OrderId, CancellationToken.None);
            if (order == null)
            {
                return OrderResponse.Error("Order not found");
            }

            // Lấy thông tin giao dịch
            var transaction = await transactionRepository.GetAsync(o => o.DocNo.ToString().Contains(order.OrderId.ToString()), CancellationToken.None);
            if (transaction == null)
            {
                return OrderResponse.Error("Transaction not found");
            }

            // Kiểm tra trạng thái đơn hàng
            if (order.Status.ToLower() != OrderStatus.Pending.ToString().ToLower())
            {
                return OrderResponse.Error("Only pending orders can be canceled");
            }

            // Lấy danh sách OrderDetail của đơn hàng
            var orderDetails = await orderDetailRepository.FindAsync(od => od.OrderId == order.OrderId);
            if (!orderDetails.Any())
            {
                return OrderResponse.Error("No order details found");
            }

            // Cập nhật trạng thái đơn hàng
            order.UpdatedDate = DateTime.UtcNow;
            order.Status = OrderStatus.Cancel.ToString();
            order.Note = request.reason;

            // Hoàn lại số lượng sản phẩm vào kho
            foreach (var orderDetail in orderDetails)
            {
                var product = await productRepository.GetAsync(p => p.ProductId == orderDetail.ProductId, CancellationToken.None);
                if (product != null)
                {
                    product.StockQuantity += orderDetail.Quantity; // Hoàn lại số lượng
                    productRepository.Update(product);
                }
            }

            // Xử lý hoàn tiền nếu có thanh toán
            if (transaction.Payment != null)
            {
                var refundInfo = new RefundInfo
                {
                    Amount = (decimal)(order.Total + long.Parse(order.ShipFee)),
                    Date = DateTime.UtcNow,
                    Description = $"Hoàn Tiền cho hóa đơn bị hủy {order.OrderId}"
                };
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string refundJson = JsonSerializer.Serialize(refundInfo, jsonOptions);
                transaction.Refund = refundJson;

                // Cập nhật số dư ví
                var wallet = await walletRepository.GetAsync(x => x.UserId.Equals(order.UserId), CancellationToken.None);
                if (wallet != null)
                {
                    wallet.Amount += (order.Total + long.Parse(order.ShipFee));
                    walletRepository.Update(wallet);
                }
            }

            // Cập nhật giao dịch
            transaction.TransactionType = TransactionType.Cancel.ToString();
            transactionRepository.Update(transaction);

            // Cập nhật đơn hàng
            orderRepository.Update(order);

            // Lưu tất cả thay đổi
            await uow.SaveChangesAsync();

            return OrderResponse.Success("Order canceled successfully");
        }
        catch (Exception ex)
        {
            return OrderResponse.Error($"Failed to cancel order: {ex.Message}");
        }
    }

    public async Task<List<OrderFilterResponse>> GetAllOrders(CancellationToken cancellationToken = default)
    {
        // Retrieve all orders with related entities
        var result = (await orderRepository.FindAsync(
            predicate: _ => true, // No specific filter to get all orders
            include: u => u.Include(u => u.Shop)
                           .Include(u => u.User)
                           .Include(u => u.OrderDetail)
                           .Include(u => u.Report),
            cancellationToken
        )).OrderByDescending(u => u.CreatedDate).ToList();

        // Retrieve transactions for all orders
        var transactionList = await transactionRepository.FindAsync(
            t => result.Select(o => o.OrderId).Contains(t.DocNo),
            cancellationToken
        );

        // Retrieve user membership data
        var mem = await memRepository.FindAsync(
            u => result.Select(u => u.UserId).Contains(u.UserId),
            cancellationToken
        );

        // Map orders to response DTO
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
                TransactionInfo = transaction != null ? new TransactionDto
                {
                    TransactionId = transaction.TransactionId,
                    TransactionDate = transaction.TransactionDate,
                    TransactionType = transaction.TransactionType,
                    VnPayTransactionId = transaction.VnPayTransactionid,
                    DocNo = transaction.DocNo,
                    Payment = !string.IsNullOrEmpty(transaction.Payment)
                        ? JsonSerializer.Deserialize<PaymentInfo>(transaction.Payment)
                        : null,
                    Refund = !string.IsNullOrEmpty(transaction.Refund)
                        ? JsonSerializer.Deserialize<RefundInfo>(transaction.Refund)
                        : null
                } : null,
                ReportDetail = u.Report != null ? new ReportDetailResponse
                {
                    ReportId = u.Report.ReportId,
                    CreatedDate = u.Report.CreatedDate,
                    image = u.Report.image,
                    status = u.Report.status,
                    Reason = u.Report.Reason,
                    OrderId = u.Report.OrderId
                } : null,
                Details = u.OrderDetail.Select(d => new OrderDetailDto
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity
                }).ToList()
            };
        }).ToList();
    }

    public async Task<List<InvoiceDetailResponse>> CalculateOrderInvoicesAsync(CreateOrderRequest request)
    {
        try
        {
            if (request == null || request.OrderDetails == null || !request.OrderDetails.Any())
            {
                throw new ArgumentException("Invalid request data");
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

            // Lấy danh sách Product trước (tránh gọi DB nhiều lần)
            var productIds = request.OrderDetails.Select(d => d.ProductId).ToList();
            var products = await productRepository.FindAsync(x => productIds.Contains(x.ProductId));

            // Nhóm sản phẩm theo ShopId
            var groupedOrderDetails = request.OrderDetails
                .GroupBy(d => products.FirstOrDefault(p => p.ProductId == d.ProductId)?.ShopId ?? Guid.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<InvoiceDetailResponse> invoiceResponses = new List<InvoiceDetailResponse>();

            foreach (var group in groupedOrderDetails)
            {
                Guid shopId = group.Key;
                var orderDetails = group.Value;
                var shop = await shopRepository.GetAsync(x => x.ShopId.Equals(shopId), CancellationToken.None);

                // Tính tổng giá trị đơn hàng và tổng trọng lượng cho từng ShopId
                decimal totalProductPrice = 0;
                int totalWeight = 0;
                var items = new List<OrderItemResponse>();

                foreach (var detail in orderDetails)
                {
                    var product = products.FirstOrDefault(p => p.ProductId == detail.ProductId);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product with ID {detail.ProductId} not found");
                    }

                    if (product.StockQuantity < detail.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product {product.ProductName}");
                    }

                    decimal itemTotal = product.Price * detail.Quantity;
                    totalProductPrice += itemTotal;
                    totalWeight += (int)product.Weight * detail.Quantity;

                    items.Add(new OrderItemResponse
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        Quantity = detail.Quantity,
                        UnitPrice = product.Price,
                        TotalItemPrice = itemTotal
                    });
                }

                int districtId;
                if (!int.TryParse(request.Address.DistrictId, out districtId))
                {
                    throw new ArgumentException("Invalid DistrictId format. Must be a valid integer.");
                }

                var feeRequest = new GHNShippingFeeReuqest
                {
                    service_type_id = 2,
                    to_district_id = districtId,
                    to_ward_code = request.Address.WardId.ToString(),
                    weight = totalWeight,
                    length = 10, // Giả sử giá trị mặc định
                    width = 10,
                    height = 10,
                    insurance_value = 0,
                    coupon = "",
                    items = orderDetails.Select(d => new Item
                    {
                        name = products.FirstOrDefault(p => p.ProductId == d.ProductId)?.ProductName ?? "Product",
                        quantity = d.Quantity,
                        weight = (int)products.FirstOrDefault(p => p.ProductId == d.ProductId).Weight,
                    }).ToList()
                };

                var shippingFeeResponse = await ghnService.CalculateShippingFee(feeRequest, shop.GHNId);
                if (shippingFeeResponse == null)
                {
                    throw new InvalidOperationException("Không thể tính phí vận chuyển");
                }
                decimal shippingFee = shippingFeeResponse.Data.Total;

                // Tạo response cho hóa đơn của cửa hàng này
                invoiceResponses.Add(new InvoiceDetailResponse
                {
                    ShopId = shopId,
                    ShopName = shop?.ShopName ?? "Unknown Shop",
                    TotalProductPrice = totalProductPrice,
                    ShippingFee = shippingFee,
                    TotalOrderPrice = totalProductPrice + shippingFee,
                    Items = items
                });
            }

            return invoiceResponses;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create order: {ex.Message}", ex);
        }
    }
}
