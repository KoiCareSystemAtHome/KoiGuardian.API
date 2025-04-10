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
        public AddressDto ShopAddress { get; set; }
        public string GhnId { get; set; }
        public bool IsActivate { get; set; }
        public string BizLicences { get; set; }

       
    }

    public class ShopRequestDetails
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
        public decimal ShopRate { get; set; }
        public string ShopDescription { get; set; }
        public AddressDto ShopAddress { get; set; }
        public bool IsActivate { get; set; }
        public string BizLicences { get; set; }
        public string? ShopAvatar { get; set; }
        public List<ProductDetailsRequest> Products { get; set; }


    }



}
