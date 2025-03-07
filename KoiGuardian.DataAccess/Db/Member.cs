using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class Member
{
    [Key]
    public string MemberId { get; set; }
    public string UserId { get; set; }

    public string Avatar {  get; set; }
    public string Gender {  get; set; }
    public string Address {  get; set; }
}
