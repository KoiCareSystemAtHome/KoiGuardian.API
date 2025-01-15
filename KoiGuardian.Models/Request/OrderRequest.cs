using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request;

public class OrderFilterRequest
{
    public string? AccountId { get; set; }
    public string RequestStatus { get; set; }
    public string SearchKey { get; set; }
}
