using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class BlogProduct
    {
        public Guid BPId { get; set; }

        public Guid BlogId { get; set; }
        public virtual Blog Blog { get; set; }

        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; }
    }
}
