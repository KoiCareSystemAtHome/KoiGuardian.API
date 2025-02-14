using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class MedicineDisease
    {
        public Guid MedicineDiseaseId {get; set; }
        public Guid MedinceId { get; set; }
        public Guid DiseaseId { get; set; }

       public virtual Disease Disease { get; set; }
       public virtual Medicine Medince { get; set; }
    }

}
