using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request;

public class DiseaseTypePredictRequest
{
    public Guid SymtompId { get; set; }
    public string? Value { get; set; }
}
