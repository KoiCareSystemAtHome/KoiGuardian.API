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

        public string ProductName { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string Image { get; set; }

        public Guid CategoryId { get; set; }

        public string Brand { get; set; }

        public DateTime ManufactureDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        [JsonConverter(typeof(ParameterImpactConverter))]
        public Dictionary<string, ParameterImpactType> ParameterImpacts { get; set; }

        public Guid ShopId { get; set; }

        public ProductType Type { get; set; }

    }

    public class ProductUpdateRequest
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string Image { get; set; }

        public Guid CategoryId { get; set; }

        public string Brand { get; set; }

        public DateTime ManufactureDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        [JsonConverter(typeof(ParameterImpactConverter))]
        public Dictionary<string, ParameterImpactType> ParameterImpacts { get; set; }

        public Guid ShopId { get; set; }

    }

    public class ProductDetailsRequest
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string Image { get; set; }

        public Guid CategoryId { get; set; }

        public string Brand { get; set; }

        public DateTime ManufactureDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        [JsonConverter(typeof(ParameterImpactConverter))]
        public Dictionary<string, ParameterImpactType> ParameterImpacts { get; set; }

        public Guid ShopId { get; set; }

    }

    public class FoodRequest : ProductRequest
    {
        public string Name { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }

        public FoodRequest()
        {
            Type = ProductType.Food; // Đặt mặc định là Food
        }

       
    }
    public class MedicineRequest : ProductRequest
    {
        public string MedicineName { get; set; }
        public string DosageForm { get; set; }
        public string Symptoms { get; set; }

        public Guid? PondParamId { get; set; }

        public MedicineRequest()
        {
            Type = ProductType.Medicine; // Đặt mặc định là Medicine
        }
    }

    public class FoodUpdateRequest : ProductUpdateRequest
    {
        public string Name { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
    }


    public class MedicineUpdateRequest : ProductUpdateRequest
    {
        public string MedicineName { get; set; }
        public string DosageForm { get; set; }
        public string Symptoms { get; set; }
    }



}
