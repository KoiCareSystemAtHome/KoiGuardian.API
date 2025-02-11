using KoiGuardian.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace KoiGuardian.DataAccess.Db
{
    public class Product
    {

        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Brand { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        public string ParameterImpactment { get; set; }
        public Guid ShopId { get; set; }

        public virtual Shop Shop { get; set; }

        public Guid CategoryId { get; set; }
        public ProductType Type { get; set; }
        public bool? FoodIsFloat { get; set; }
        public int AgeFrom { get; set; } // từ số tháng tuổi 
        public int AgeTo { get; set; } // tới số tháng tuồi
        public int ProductWeight { get; set; } // tới cân nặng tính theo gram để so sánh mua không bị dư quá đà nè sốp


        public virtual Category Category { get; set; }

        public Dictionary<string, ParameterImpactType> GetParameterImpacts()
        {
            return string.IsNullOrEmpty(ParameterImpactment)
                ? new Dictionary<string, ParameterImpactType>()
                : JsonSerializer.Deserialize<Dictionary<string, ParameterImpactType>>(ParameterImpactment);
        }

        public void SetParameterImpacts(Dictionary<string, ParameterImpactType> impacts)
        {
            ParameterImpactment = JsonSerializer.Serialize(impacts);
        }

        public virtual ICollection<BlogProduct> BlogProducts { get; set; } = new List<BlogProduct>();

        public virtual IEnumerable<Feedback> Feedbacks { get; set; }
        public virtual IEnumerable<RelMedicineProduct> RelMedicineProducts { get; set; }
    }

    public enum ProductType
    {
        Food,
        Pond_Equipment,
        Medicine,
        Funtional_Food
    }
}
