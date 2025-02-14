using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class Food
    {
        [Key]
        public Guid FoodId { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; }    
        public int AgeFrom { get; set; }    
        public int AgeTo { get; set; }


        public virtual Product Product { get; set; }
    }
}
