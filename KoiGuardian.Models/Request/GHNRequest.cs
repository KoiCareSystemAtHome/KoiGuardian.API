using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class GHNRequest
    {
        public string to_name { get; set; }
        public string from_name { get; set; }
        public string from_phone { get; set; }
        public string from_address { get; set; }
        public string from_ward_name { get; set; }
        public string from_district_name { get; set; }
        public string from_province_name { get; set; }
        public string to_phone { get; set; }
        public string to_address { get; set; }
        public string to_ward_code { get; set; }
        public int to_district_id { get; set; }
        public int weight { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int service_type_id { get; set; }
        public int payment_type_id { get; set; }
        public string required_note { get; set; }
        public List<Item> items { get; set; }
    }

    public class Item
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public int weight { get; set; }
    }

}
