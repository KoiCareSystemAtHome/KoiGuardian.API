using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class ShopRequest
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
        public decimal ShopRate { get; set; }
        public string ShopDescription { get; set; }
        public string ShopAddress { get; set; }
        public bool IsActivate { get; set; }
        public string BizLicences { get; set; }

        public List<ProductRequest> Products { get; set; }
    }

  


}
