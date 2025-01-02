using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Category
    {
        public Guid CategoryId { get; set; }    
        public Guid ShopId { get; set; }
        public string Name { get; set; }
        public String Description { get; set; }

        public virtual ICollection<Product> Products { get; set; }
        public virtual Shop Shop { get; set; }
        }
}
