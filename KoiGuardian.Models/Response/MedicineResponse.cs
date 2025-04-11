using KoiGuardian.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class MedicineResponse
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
        public string ParameterImpactment { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; }
        public ProductType Type { get; set; }

        // Medicine-specific fields
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string DosageForm { get; set; }
        public string Symptoms { get; set; }
        public Guid? PondParamId { get; set; }

        // Feedback data
        public int FeedbackCount { get; set; }
        public double AverageRating { get; set; }

        public Guid FeedbackId { get; set; }

        public string Content { get; set; }
    }


    public class RecommendResponse
    {
        public List<MedicineResponse> Medicines { get; set; } = new();
    }
}
