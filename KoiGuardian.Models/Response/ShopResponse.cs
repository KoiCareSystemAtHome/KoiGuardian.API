using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class ShopResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }

        public ShopDTO Shop { get; set; }

    }

    public class ShopDTO
    {
        public string ShopId { get; set; }
        public string ShopName { get; set; }
        public decimal ShopRate { get; set; }
        public string ShopDescription { get; set; }
        public string ShopAddress { get; set; }
        public bool IsActivate { get; set; }
        public string BizLicences { get; set; }
    }



}
