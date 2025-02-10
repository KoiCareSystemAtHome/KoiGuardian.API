using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class Article
{
    public Guid Id { get; set; }
    public string Link { get; set; }
    public string Title { get; set; }
    public bool isSeen { get; set; }
    public DateTime CrawDate { get; set; }
}
