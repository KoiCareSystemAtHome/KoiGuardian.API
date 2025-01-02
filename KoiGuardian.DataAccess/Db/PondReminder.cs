using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class PondReminder
{
    public Guid PondReminderId { get; set; }
    public Guid PondId { get; set; }
    public ReminderType ReminderType { get; set; }
    public string Title { get; set; }   
    public string Description { get; set; }
    public DateTime MaintainDate { get; set; }
    public DateTime SeenDate { get; set; }

    public virtual Pond? Pond { get; set; }

}


public enum ReminderType
{
    Fish,
    Pond,

}