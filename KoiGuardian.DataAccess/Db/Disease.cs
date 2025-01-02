﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class Disease
{
    public Guid DiseaseId { get; set; }
    public Guid VarietyId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DiseaseType Type { get; set; }

    public virtual Variety? Variety { get; set; }   
    public virtual IEnumerable<Medicine>? Medicine { get; set; }
}

public enum DiseaseType
{
    Common,
    Variety_only
}
