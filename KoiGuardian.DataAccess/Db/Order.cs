

namespace KoiGuardian.DataAccess.Db
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public Guid ShopId { get; set; }
        public string UserId  { get; set; }
        public string ShipType { get; set; }
        public string oder_code { get; set; }
        public string Status { get; set; }
        public string ShipFee { get; set; } // include currencies
        public string Note { get; set; }
        public float Total { get; set; }

        public virtual ICollection<OrderDetail> OrderDetail { get; set; }
        public virtual Shop Shop { get; set; }
        public virtual User User { get; set; }
    }

    public enum OrderStatus
    {
        Pending, 
        Confirm,
        Complete,
        Inprogress
    }
}
