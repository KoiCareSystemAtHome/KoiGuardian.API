using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class RelMedicineProduct
    {
        public Guid RelMedicineProductId { get; set; }
        public Guid MedinceId { get; set; }
        public Guid ProductId { get; set; }

       public virtual Product Product { get; set; }
       public virtual Medicine Medince { get; set; }
    }

}
