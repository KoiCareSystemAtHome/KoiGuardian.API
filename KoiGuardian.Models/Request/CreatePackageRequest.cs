using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Request
{
    public class CreatePackageRequest
    {   
        public string PackageTitle { get; set; } = string.Empty;

        public string PackageDescription { get; set; } = string.Empty;

        public decimal PackagePrice { get; set; }

        public string Type { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }


    public class UpdatePackageRequest
    {
        public Guid PackageId { get; set; }

        public string PackageTitle { get; set; } = string.Empty;

        public string PackageDescription { get; set; } = string.Empty;

        public decimal PackagePrice { get; set; }

        public string Type { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }

    public class FilterPackageRequest
    {

        public string? SeachKey { get; set; } = string.Empty;

        public decimal? PackagePrice { get; set; }

        public string? Type { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

}

