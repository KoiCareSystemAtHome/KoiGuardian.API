using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class KoiReport

{
    [Key]
    public Guid KoiReportId {  get; set; }
    public Guid KoiId {  get; set; }

    public DateTime CalculatedDate { get; set; }

    public float Weight  { get; set; }
    public float Size { get; set; }

    public Fish? Fish { get; set; }
}
