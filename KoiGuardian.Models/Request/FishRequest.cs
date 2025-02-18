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
        public string VarietyName { get; set; }
        public DateTime InPondSince { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public float size { get; set; }
        public float weight { get; set; }
    }

  

    public class FishInfo
    {
        public Guid FishId { get; set; }
        public string FishName { get; set; }
    }
}