using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Range(1, 50000, ErrorMessage = "Weight must be between 1 and 50000.")]//this mean maximum 50000gram
        public int weight { get; set; }
        [Range(1, 200, ErrorMessage = "Weight must be between 1 and 200.")]//this mean maximum 200cm
        public int length { get; set; }
        [Range(1, 200, ErrorMessage = "Weight must be between 1 and 200.")]//this mean maximum 200cm
        public int width { get; set; }
        [Range(1, 200, ErrorMessage = "Weight must be between 1 and 200.")]//this mean maximum 200cm
        public int height { get; set; }
        [AllowedValues(2, 5, ErrorMessage = "Service type ID must be either 2 or 5.")]//Default value: 2: E-commerce Delivery, 5: Traditional Delivery
        public int service_type_id { get; set; }
        [AllowedValues(1, 2, ErrorMessage = "Payment type ID must be either 1 or 2.")]//Choose Who pay option 1: Shop/Seller. 2: Buyer/Consignee.
        public int payment_type_id { get; set; }
        //only have 3 option : CHOTHUHANG, CHOXEMHANGKHONGTHU, KHONGCHOXEMHANG
        public string required_note { get; set; }
        //this require when service_type_id you choose 5
        public List<Item> items { get; set; }
    }

    public class Item
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public int weight { get; set; }
    }

    public class CancelOrderRequest
    {
        [JsonProperty("order_codes")]
        public List<string> OrderCodes { get; set; }
    }

    public class GHNShopRequest
    {
        [JsonProperty("district_id")]
        public int DistrictId { get; set; }

        [JsonProperty("ward_code")]
        public string WardCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }

}
