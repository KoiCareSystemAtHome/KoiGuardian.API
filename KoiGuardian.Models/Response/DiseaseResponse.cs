using KoiGuardian.Models.Enums;
using KoiGuardian.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class DiseaseResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }

        public Guid DiseaseId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DiseaseType Type { get; set; }
        public float FoodModifyPercent { get; set; }
        public float SaltModifyPercent { get; set; }
        public string Image {  get; set; }
        public List<MedicineDTO>? Medicines { get; set; }
    }

   

    public class MedicineDTO
    {
        public Guid MedicineId { get; set; }
        public string Name { get; set; }
       
    }
}
