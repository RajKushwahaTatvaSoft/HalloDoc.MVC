﻿using Business_Layer.Helpers;
using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System;
using System.Security.Cryptography;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Business_Layer.Repository
{
    public class RequestRepository : Repository<Request>, IRequestRepository
    {
        private ApplicationDbContext _context;
        public RequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
