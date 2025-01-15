﻿using KoiGuardian.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiGuardian.Models.Response
{
    public class ShopResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }

        public ShopRequestDetails Shop { get; set; }

    }

    



}
