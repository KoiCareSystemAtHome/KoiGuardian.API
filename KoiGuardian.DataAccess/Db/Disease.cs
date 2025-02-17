using KoiGuardian.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.DataAccess.Db;

public class Disease
{
    public Guid DiseaseId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DiseaseType Type { get; set; }
    public float FoodModifyPercent { get; set; }
    public float SaltModifyPercent { get; set; }

    public virtual IEnumerable<MedicineDisease> MedicineDisease {  get; set; }
}


