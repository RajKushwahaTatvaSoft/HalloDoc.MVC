﻿using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    internal class BusinessRepo : Repository<Business>, IBusinessRepo
    {
        private ApplicationDbContext _context;
        public BusinessRepo(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}