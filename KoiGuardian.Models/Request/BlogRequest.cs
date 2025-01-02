﻿using System;
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
}
