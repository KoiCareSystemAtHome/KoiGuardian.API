using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request;

public class OrderFilterRequest
{
    public string? AccountId { get; set; }
   /* public string RequestStatus { get; set; }
    public string SearchKey { get; set; }*/
}

public class UpdateOrderRequest
{
    public Guid OrderId { get; set; }  // ID của đơn hàng cần cập nhật
    public AddressDto Address { get; set; }  // Địa chỉ giao hàng (được lưu dưới dạng JSON string)
    public string ShipType { get; set; }  // Loại vận chuyển
    public string Status { get; set; }  // Trạng thái đơn hàng
    public decimal ShipFee { get; set; }  // Phí vận chuyển
    public List<OrderDetailDto> OrderDetails { get; set; }  // Danh sách sản phẩm trong đơn hàng
}

public class CreateOrderRequest
{
   // public Guid ShopId { get; set; }
    public string AccountId { get; set; }
    public string ShipType { get; set; }
    public string Status { get; set; }
    public decimal ShipFee { get; set; } // Lưu dạng số
    public string Note { get; set; }
    public AddressDto Address { get; set; } // Đối tượng Address
    public List<OrderDetailDto> OrderDetails { get; set; }
}

public class AddressDto
{
    public string ProvinceId { get; set; }
    public string ProvinceName { get; set; }
    public string DistrictId { get; set; }
    public string DistrictName { get; set; }
    public string WardId { get; set; }
    public string WardName { get; set; }
}

public class UpdateOrderStatusRequest
{
    public Guid OrderId { get; set; }  // ID của đơn hàng cần cập nhật
    public string Status { get; set; }  // Trạng thái đơn hàng
}

public class UpdateOrderCodeShipFeeRequest
{
    public Guid OrderId { get; set; }  // ID của đơn hàng cần cập nhật
    public string order_code { get; set; }  // Trạng thái đơn hàng
    public string ShipFee { get; set; }  // Trạng thái đơn hàng
}

/*public class UpdateOrderShipFeeRequest
{
    public Guid OrderId { get; set; }  // ID của đơn hàng cần cập nhật
    public string ShipFee { get; set; }  // Trạng thái đơn hàng
}*/
public class UpdateOrderShipTypeRequest
{
    public Guid OrderId { get; set; }  // ID của đơn hàng cần cập nhật
    public string ShipType { get; set; }  // Trạng thái đơn hàng
}

public class RejectOrderRequest
{
    public Guid OrderId { get; set; }  // ID của đơn hàng cần cập nhật
    //public string Status { get; set; }  // Trạng thái đơn hàng
    public string reason { get; set; }
}