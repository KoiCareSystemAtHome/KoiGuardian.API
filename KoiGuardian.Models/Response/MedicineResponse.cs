using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class MedicineResponse
    {
        public Guid MedicineId { get; set; }
        public string Medicinename { get; set; }
        public string DosageForm { get; set; }
        public string Symtomps { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public double FeedbackCount { get; set; }

        public double AverageRating { get; set; }
    }

    public class RecommendResponse
    {
        public List<MedicineResponse> Medicines { get; set; } = new();
    }
}
