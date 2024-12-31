using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class BlogProduct
    {
        public string BPId { get; set; }

        public string BlogId { get; set; }
        public virtual Blog Blog { get; set; }

        public string ProductId { get; set; }
        public virtual Product Product { get; set; }
    }
}
