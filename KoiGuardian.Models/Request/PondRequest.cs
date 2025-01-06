using Microsoft.AspNetCore.Http;
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
        public IFormFile Image { get; set; }

        public required List <PondParam> RequirementPondParam {  get; set; } 

    }
    public class PondParam
    {
        //public Guid PondID { get; set; }
        public Guid ParamterUnitID { get; set; }    
        public float Value { get; set; }
    }



    public class UpdatePondRequest
    {
        public Guid PondID { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
