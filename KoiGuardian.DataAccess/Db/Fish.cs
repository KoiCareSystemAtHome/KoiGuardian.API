using System;

namespace KoiGuardian.DataAccess.Db
{
    public class Fish
    {
        public Guid KoiID { get; set; } 
        public Guid PondID { get; set; } 
        public Guid VarietyId { get; set; } 
        public string Name { get; set; }
        public string Image { get; set; }
        public DateTime InPondSince { get; set; } 
        public decimal Price { get; set; }
        public string Sex { get; set; }
        public string Physique { get; set; }
        public string Breeder { get; set; }
        public int Age { get; set; } // số tháng tuổi
        public string Notes { get; set; } = "[]";// số tháng tuổi

        public virtual Pond? Pond { get; set; }
        public virtual Variety? Variety { get; set; }
        public virtual IEnumerable<KoiReport> RelKoiParameters { get; set; }
    }
}
