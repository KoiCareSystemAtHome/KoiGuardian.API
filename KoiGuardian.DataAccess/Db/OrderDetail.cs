﻿namespace KoiGuardian.DataAccess.Db
{
    public class OrderDetail
    {
        public Guid OderDetailId { get; set; }
        public Guid OderId { get; set; }
        public Guid ProductId { get; set; }

        public virtual Product Product { get; set; }
        public virtual Order Order { get; set; }
        public virtual IEnumerable<Feedback> Feedbacks { get; set; }
    }
}
