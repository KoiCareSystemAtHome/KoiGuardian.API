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
        public Guid ProductId { get; set; }
        public string Medicinename { get; set; }
        public string DosageForm { get; set; }
        public string Symtomps { get; set; }

        public virtual Product? Product { get; set; }
        public virtual IEnumerable<MedicineDisease>? MedicineDisease { get; set; }
    }
}
