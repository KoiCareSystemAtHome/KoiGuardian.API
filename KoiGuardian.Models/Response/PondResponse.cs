﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class PondResponse
    {
        public string status { get; set; }
        public string message { get; set; }
    }

    public class PondDto
    {
        public Guid PondID { get; set; }
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public DateTime CreateDate { get; set; }
        public string Image { get; set; }
        public float MaxVolume { get; set; }
        public float FishAmount { get; set; }
        //public List<FishDto> Fish { get; set; }
        public string Status { get; set; }
        public string StatusDescription { get; set; }
    }
}
