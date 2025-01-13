using KoiGuardian.Models.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class ProductRequest
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string Image {  get; set; }

        public Guid CategoryId { get; set; }

        public string Brand { get; set; }

        public DateTime ManufactureDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        [JsonConverter(typeof(ParameterImpactConverter))]
        public Dictionary<string, ParameterImpactType> ParameterImpacts { get; set; }

        public Guid ShopId { get; set; }

    }
}
