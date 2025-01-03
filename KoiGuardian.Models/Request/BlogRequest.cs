using System;
using System.Collections.Generic;

namespace KoiGuardian.Models.Request
{
    public class BlogRequest
    {
        public Guid BlogId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string Images { get; set; }

        public string Tag { get; set; }

        public bool IsApproved { get; set; } = false;

        public string Type { get; set; }

        public string ReportedBy { get; set; }

        public Guid ShopId { get; set; }

        public List<Guid> ProductIds { get; set; } = new List<Guid>();

        public DateTime? ReportedDate { get; set; }
    }

    public class BlogDto
    {
        public Guid BlogId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Images { get; set; }
        public string Tag { get; set; }
        public bool IsApproved { get; set; }
        public string Type { get; set; }
        public string ReportedBy { get; set; }
        public DateTime? ReportedDate { get; set; }
        public int View { get; set; }

        // Shop information
        public Guid ShopId { get; set; }
        public ShopBasicDto Shop { get; set; }

        // Related products
        public List<ProductBasicDto> Products { get; set; } = new List<ProductBasicDto>();
    }

    public class ShopBasicDto
    {
        public Guid ShopId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
    }

    public class ProductBasicDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
    }
}
