using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class FeedbackRequest
    {
        public Guid FeedbackId { get; set; }
        public Guid ProductId { get; set; }
        public Guid MemberId { get; set; }
        public int Rate { get; set; }
        public string Content { get; set; }
    }
}
