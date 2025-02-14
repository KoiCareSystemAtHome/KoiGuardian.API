using KoiGuardian.Models.Request;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db
{
    public class MedicinePondParameter
    {
        [Key]
        public Guid MedicinePondParameterId { get; set; }
        public Guid MedicineId { get; set; }
        public Guid PondParamId { get; set; }

        public virtual Medicine Medicine { get; set; }
        public virtual PondStandardParam PondParam { get; set; }
    }
}
