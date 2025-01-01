using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Variety
    {
        public Guid VarietyId { get; set; } 

        public string VarietyName { get; set; }
        public string Description { get; set; }
        public Guid CreatedBy { get; set; }


        public virtual User? Author { get; set; }    
    }
}
