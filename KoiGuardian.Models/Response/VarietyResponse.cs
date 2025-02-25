using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class VarietyResponse
    {
        public string status { get; set; }
        public string message { get; set; }
    }
    public class VarietyDto
    {
        public Guid VarietyId { get; set; }
        public string VarietyName { get; set; }
        public string Description { get; set; }
        public string AuthorId { get; set; }
    }
}
