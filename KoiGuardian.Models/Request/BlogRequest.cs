using System;
using System.Collections.Generic;

namespace KoiGuardian.Models.Request
{
    public class BlogRequest
    {
        public string BlogId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string Images { get; set; }

        public string Tag { get; set; }

        public bool IsApproved { get; set; } = false;

        public string Type { get; set; }

        public string ShopId { get; set; }

        public List<string> ProductIds { get; set; } = new List<string>();  // List of ProductIds

        public DateTime? ReportedDate { get; set; }
    }
}
