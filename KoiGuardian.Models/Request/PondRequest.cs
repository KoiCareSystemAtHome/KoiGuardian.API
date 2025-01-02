using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CreatePondRequest
    {
        //public Guid PondID { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public class UpdatePondRequest
    {
        public Guid PondID { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
