using System;

namespace KoiGuardian.DataAccess.Db
{
    public class Fish
    {
        public Guid KoiID { get; set; } 
        public int PondID { get; set; } 
        public Guid VarietyId { get; set; } 
        public string Name { get; set; }
        public string Image { get; set; }
        public DateTime InPondSince { get; set; } 
        public decimal Price { get; set; }
        public virtual Pond? Pond { get; set; }
        public virtual Variety? Variety { get; set; }
        public virtual IEnumerable<RelKoiParameter> RelKoiParameters { get; set; }
    }
}
