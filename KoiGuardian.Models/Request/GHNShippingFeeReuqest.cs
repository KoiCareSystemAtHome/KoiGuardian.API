using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class GHNShippingFeeReuqest
    {
        public int service_type_id { get; set; }
        public int to_district_id { get; set; }
        public string to_ward_code { get; set; }
        [Range(1, 50000, ErrorMessage = "Weight must be between 1 and 50000.")] //maximum 50000gram
        public int weight { get; set; }
        [Range(1, 200, ErrorMessage = "Length must be between 1 and 200.")] //maximum 200cm
        public int length { get; set; }
        [Range(1, 200, ErrorMessage = "Width must be between 1 and 200.")] //maximum 200cm
        public int width { get; set; }
        [Range(1, 200, ErrorMessage = "Height must be between 1 and 200.")] //maximum 200cm
        public int height { get; set; }
        public int insurance_value { get; set; } = 0;
        public string? coupon { get; set; }
        
        public List<Item> items { get; set; }
    }
}
