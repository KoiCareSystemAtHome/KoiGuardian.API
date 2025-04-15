using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class GHNResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public ShippingFeeData Data { get; set; }
    }

    public class ShippingFeeData
    {
        public int Total { get; set; }
        public int ServiceFee { get; set; }
        public int InsuranceFee { get; set; }
    }
}
