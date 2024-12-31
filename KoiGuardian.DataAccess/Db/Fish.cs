using System;

namespace KoiGuardian.DataAccess.Db
{
    public class Fish
    {
        public int KoiID { get; set; } 
        public int PondID { get; set; } 
        public string Name { get; set; }
        public string Image { get; set; }
        public string Variety { get; set; } 
        public DateTime InPondSince { get; set; } 
        public decimal Price { get; set; }
        public virtual Pond Pond { get; set; }
    }
}
