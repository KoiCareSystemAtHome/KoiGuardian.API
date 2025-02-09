﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class CalculateSaltResponse
    {
        public Guid PondId { get; set; }
        public double TotalSalt { get; set; }
        public double? ExcessSalt { get; set; }        // Amount of excess salt
        public double? WaterNeeded { get; set; }       // Amount of water needed to dilute
        public List<string> AdditionalInstruction { get; set; } = new List<string>();
    }

    public class AddSaltResponse
    {
        public bool CanAddSalt { get; set; }
        public double AllowedSaltWeightKg { get; set; }
        public DateTime? NextAllowedTime { get; set; }
        public List<string> Messages { get; set; } = new();
    }
}
