using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Pond
    {
        public Guid PondID { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public string Image { get; set; }


        public virtual ICollection<Fish> Fish { get; set; }
        public virtual IEnumerable<RelPondParameter>? RelPondParameter { get; set; }

    }
}
