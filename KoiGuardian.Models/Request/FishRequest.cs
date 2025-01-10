using Microsoft.AspNetCore.Http;
using System;

namespace KoiGuardian.Models.Request
{
    public class FishRequest
    {
        public Guid? KoiID { get; set; }
        public Guid PondID { get; set; }
        public string Name { get; set; }
        public string Physique { get; set; }
        public decimal Length { get; set; }  
        public string Sex { get; set; }
        public string Breeder { get; set; }
        public int Age { get; set; } // số tháng tuổi
        public decimal Weight { get; set; } 
        public string VarietyName { get; set; }
        public DateTime InPondSince { get; set; }
        public IFormFile Image { get; set; }
        public decimal Price { get; set; }
        public required List<FishParam> RequirementFishParam { get; set; }
    }

    public class FishParam
    {
        public Guid ParamterUnitID { get; set; }
        public float Value { get; set; }
    }
}