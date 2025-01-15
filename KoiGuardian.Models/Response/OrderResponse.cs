
namespace KoiGuardian.Models.Response;

public class OrderFilterResponse
{
    public Guid OrderId { get; set; }
    public string ShopName { get; set; }
    public string CustomerName { get; set; }
    public string CustomerAddress { get; set; }
    public string CustomerPhoneNumber { get; set; }
    public string ShipType { get; set; }
    public string oder_code { get; set; }
    public string Status { get; set; }
    public string ShipFee { get; set; } // include currencies
    public string Note { get; set; }
}

public class OrderDetailResponse
{
    public Guid OrderId { get; set; }
    public string ShopName { get; set; }
    public string CustomerName { get; set; }
    public string CustomerAddress { get; set; }
    public string CustomerPhoneNumber { get; set; }
    public string ShipType { get; set; }
    public string oder_code { get; set; }
    public string Status { get; set; }
    public string ShipFee { get; set; } // include currencies
    public string Note { get; set; }

    public object Details { get; set; }
}
