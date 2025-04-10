using System;
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
        public double CurrentSalt { get; set; }
        public double SaltNeeded { get; set; }
        public double WaterNeeded { get; set; }
        public List<string> AdditionalInstruction { get; set; }
        
    }

    public class SuggestedSaltReminderResponse
    {
        public Guid TemporaryId { get; set; } // ID tạm thời để frontend theo dõi
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime MaintainDate { get; set; }
    }
    public class AddSaltResponse
    {
        public bool CanAddSalt { get; set; }
        public double AllowedSaltWeightKg { get; set; }
        public DateTime? NextAllowedTime { get; set; }
        public List<string> Messages { get; set; } = new();
    }

    public class SaltAdditionProcessResponse
    {
        public Guid PondId { get; set; }
        public List<string> Instructions { get; set; }
    }
}
