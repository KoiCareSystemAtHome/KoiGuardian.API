using System;

namespace KoiGuardian.Models.Request
{
    public class FishRequest
    {
        public int KoiID { get; set; }
        public int PondID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Physique { get; set; }
        public decimal Length { get; set; }  
        public string Sex { get; set; }
        public string Breeder { get; set; }
        public int Age { get; set; }
        public decimal Weight { get; set; } 
        public Guid Variety { get; set; }
        public DateTime InPondSince { get; set; }
        public decimal Price { get; set; }
    }
}