using KoiGuardian.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class FishResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
         public FishDto? Fish { get; set; }
    }

    public class FishDto
    {
        public Guid KoiID { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public string Sex { get; set; }
        public int Age { get; set; }
        public object? Pond { get; set; }
        public object? Variety { get; set; }
        public object? DiseaseTracking { get; set; }
        public List<FishReportInfo> fishReportInfos { get; set; }
    }
}
