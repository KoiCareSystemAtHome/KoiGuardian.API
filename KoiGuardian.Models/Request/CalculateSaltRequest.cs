using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CalculateSaltRequest
    {

            public Guid PondId { get; set; }
            public string StandardSaltLevel { get; set; }

        }
    
}
