﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CreatePondRequest
    {
        //public Guid PondID { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public string Image { get; set; }
        public float MaxVolume { get; set; }
        public required List<PondParam> RequirementPondParam { get; set; }

    }
    public class PondParam
    {
        //public Guid PondID { get; set; }
        public Guid HistoryId { get; set; }    
        public float Value { get; set; }
    }



    public class UpdatePondRequest
    {
        public Guid PondID { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public string Image { get; set; }
        public float MaxVolume { get; set; }

        public required List<PondParam> RequirementPondParam { get; set; }
    }


    public class PondDetailResponse
    {
        public Guid PondID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public DateTime CreateDate { get; set; }
        public string OwnerId { get; set; }
        public float MaxVolume { get; set; }
        public List<PondParameterInfo> PondParameters { get; set; }
        public List<FishInfo> Fish { get; set; }
        public List<ProductRecommentInfo> Recomment { get; set; }
    }

    

  

    public class ProductRecommentInfo
    {
        public Guid Productid  { get; set; }
    }

    public class UpdatePondIOTRequest
    {
        public Guid PondID { get; set; }
        public required List<PondParam> RequirementPondParam { get; set; }
    }
    public class PondInfo
    {
        public Guid PondID { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public string Image { get; set; }
        public float MaxVolume { get; set; }
    }
}
