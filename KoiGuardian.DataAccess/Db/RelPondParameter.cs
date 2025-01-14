using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class RelPondParameter
    {
        public Guid RelPondParameterId { get; set; }


        public Guid PondId { get; set; }

        public Guid ParameterHistoryId { get; set; }

        public DateTime CalculatedDate { get; set; }

        public float Value { get; set; }

        public Pond? Pond { get; set; } 

        public Parameter? Parameter { get; set; }

    }
}
