using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class FishResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class FishDto
    {
        public Guid KoiID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public string Sex { get; set; }
        public int Age { get; set; }
        public PondDto Pond { get; set; }
        public VarietyDto Variety { get; set; }
    }
}
