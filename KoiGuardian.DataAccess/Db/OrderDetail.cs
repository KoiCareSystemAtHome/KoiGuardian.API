﻿using System.ComponentModel.DataAnnotations;

namespace KoiGuardian.DataAccess.Db
{
    public class OrderDetail
    {
        [Key]
        public Guid OderDetailId { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }

        public virtual Product Product { get; set; }
        public virtual Order Order { get; set; }
        
    }
}
