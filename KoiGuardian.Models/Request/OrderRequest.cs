using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request;

public class OrderFilterRequest
{
    public string? AccountId { get; set; }
    public string RequestStatus { get; set; }
    public string SearchKey { get; set; }
}

public class CreateOrderRequest
{
    public Guid ShopId { get; set; }
    public string AccountId { get; set; }
    public string ShipType { get; set; }
    public string Status { get; set; }
    public decimal ShipFee { get; set; } // Lưu dạng số
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
