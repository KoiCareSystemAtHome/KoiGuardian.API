using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class getDistrict
    {
        public int  province_id { get; set; }
    }

    public class getWard
    {
        public int district_id { get; set; }
    }
}
