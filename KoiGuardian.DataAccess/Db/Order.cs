

namespace KoiGuardian.DataAccess.Db
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public Guid ShopId { get; set; }
        public Guid AccountId { get; set; }
        public string ShipType { get; set; }
        public string oder_code { get; set; }
        public string Status { get; set; }
        public string ShipFee { get; set; } // include currencies
        public string Note { get; set; }

        public virtual ICollection<OrderDetail> OrderDetail { get; set; }
        public virtual Shop Shop { get; set; }
    }
}
