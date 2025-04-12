using KoiGuardian.Models.Enums;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class ProductResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class ProductDetailResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Brand { get; set; }
        public float Weight { get; set; }
        public string ParameterImpactment { get; set; }
        public CategoryInfo Category { get; set; }
        public ShopInfo Shop { get; set; }
        public List<FeedbackInfo> Feedbacks { get; set; }

        public ProductType Type { get; set; }
        public float Rate { get; set; }
    }

    public class CategoryInfo
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
    }

    public class ShopInfo
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
    }

    public class FeedbackInfo
    {
        public Guid FeedbackId { get; set; }
        public string MemberName { get; set; }
        public int Rate { get; set; }
        public string Content { get; set; }
    }

    public class ProductSearchResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Brand { get; set; }
        public float? Weight { get; set; }

        [JsonConverter(typeof(ParameterImpactConverter))]
        public Dictionary<string, ParameterImpactType> ParameterImpacts { get; set; }
        public string Image { get; set; }
        // Include only necessary Category information
        public CategoryInfo Category { get; set; }

        public ProductType Type { get; set; }
    }

 

    public class FoodResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Image { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Brand { get; set; }
        public float Weight { get; set; }
        [JsonConverter(typeof(ParameterImpactConverter))]
        public Dictionary<string, ParameterImpactType> ParameterImpacts { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
        public ProductType Type { get; set; }

        // Food-specific fields
        public Guid FoodId { get; set; }
        public string Name { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }

        // Feedback data
        public int FeedbackCount { get; set; }
        public double AverageRating { get; set; }

        public Guid FeedbackId { get; set; }

        public string Content { get; set; }
    }


}
