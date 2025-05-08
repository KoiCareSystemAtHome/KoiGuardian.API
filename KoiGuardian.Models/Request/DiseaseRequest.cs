using KoiGuardian.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CreateDiseaseRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DiseaseType Type { get; set; }
        public float FoodModifyPercent { get; set; }
        public float SaltModifyPercent { get; set; }
        public string? Image {  get; set; } 
        public List<Guid>? MedicineIds { get; set; }
        public List<Guid>? SideEffect { get; set; }
        public List<Guid>? SickSymtomps { get; set; }


    }

    public class UpdateDiseaseRequest
    {
        public Guid DiseaseId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DiseaseType Type { get; set; }
        public float FoodModifyPercent { get; set; }
        public float SaltModifyPercent { get; set; }

        public string? Image { get; set; }
        public List<Guid>? MedicineIds { get; set; }
    }

  
}
