using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class FishRequest
    {
        public int KoiID { get; set; }
        /*public int PondID { get; set; } */
        public string Name { get; set; }
        public string Image { get; set; }
        public string Physique { get; set; }
        public double Length { get; set; }
        public string Sex { get; set; }
        public string Breeder { get; set; }
        public int Age { get; set; }
        public double Weight { get; set; }
        public string Variety { get; set; }
        public DateTime InPondSince { get; set; }
        public decimal Price { get; set; }
    }
}
