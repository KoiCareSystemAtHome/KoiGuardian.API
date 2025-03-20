using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class PondRemiderResponse
    {
        public Guid PondReminderId { get; set; }
        public Guid PondId { get; set; }
        public string ReminderType { get; set; }
        public string PondName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime MaintainDate { get; set; }
        public DateTime SeenDate { get; set; }
    }
}
