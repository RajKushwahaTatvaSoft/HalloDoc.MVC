﻿using Business_Layer.Interface.TableInterface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.TableRepo
{
    internal class RequestConciergeRepo : Repository<Requestconcierge>, IRequestConciergeRepo
    {
        private ApplicationDbContext _context;
        public RequestConciergeRepo(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
