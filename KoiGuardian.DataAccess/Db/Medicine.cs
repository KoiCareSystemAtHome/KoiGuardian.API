using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Medicine
    {
        public Guid MedicineId { get; set; }    
        public Guid DiseaseId { get; set; }
        public string Instruction { get; set; }
        public string Data { get; set; } // List ProductID in serialization

        public virtual Disease? Disease { get; set; }
        public virtual IEnumerable<RelMedicineProduct> RelMedicineProducts { get; set; }

    }
}
