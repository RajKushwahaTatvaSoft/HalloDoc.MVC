﻿using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class EmailLogsViewModel
    {
        public IEnumerable<Role> roles { get; set; }
        public IEnumerable<Emaillog> emailLogs {  get; set; }
    }
}