﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class KoiDiseaseProfile
{
    public Guid KoiDiseaseProfileId { get; set; }
    public Guid? DiseaseId { get; set; }
    public Guid? MedicineId { get; set; }
    public Guid FishId { get; set; }
    public DateTime Createddate { get; set; }
    public DateTime EndDate { get; set; }
    public ProfileStatus Status { get; set; }
    public string Symptoms { get; set; } // jsonb deserialization 
    public string Note { get; set; }

    public virtual Fish? Fish { get; set; }
    public virtual Disease? Disease { get; set; }
    public virtual Medicine? Medicine { get; set; }
}


public enum ProfileStatus
{
    Pending, 
    Accept,
    Buyed,
    //... 
}