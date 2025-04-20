using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class CalculateFoodResponse
    {
        public float FoodAmount {  get; set; }
        public string FeedingOften { get; set; }
        public int NumberOfFish { get; set; }
        public float TotalFishWeight { get; set; }
        public List<string> AddtionalInstruction { get; set; }
    }
}
