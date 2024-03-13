﻿using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Interface
{
    public interface IPatientDashboardRepository
    {
        public PatientDashboardViewModel FetchDashboardDetails(int id);
        public Task<PagedList<PatientDashboardRequest>> GetPatientRequestsAsync(int userId, int pageNumber, int pageSize);
    }
}
