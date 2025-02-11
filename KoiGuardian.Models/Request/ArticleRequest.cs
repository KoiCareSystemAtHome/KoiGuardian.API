using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class ArticleRequest
    {
       
        public string Link { get; set; }
        [DefaultValue(null)]
        public string? Title { get; set; }
        public bool IsSeen { get; set; }
        public DateTime CrawDate { get; set; }
    }

    public class ArticleUpdateRequest
    {
        public Guid Id { get; set; }
        public string Link { get; set; }
        public string Title { get; set; }
        public bool IsSeen { get; set; }
        public DateTime CrawDate { get; set; }
    }
}
