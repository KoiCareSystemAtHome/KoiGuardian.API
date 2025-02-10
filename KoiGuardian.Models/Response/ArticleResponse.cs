using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class ArticleResponse
    {
        public Guid Id { get; set; }
        public string Link { get; set; }
        public string Title { get; set; }
        public bool IsSeen { get; set; }
        public DateTime CrawDate { get; set; }

        public string Status { get; set; }
        public string Message { get; set; }
    }
}
