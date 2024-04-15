﻿using Data_Layer.CustomModels;
using Data_Layer.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminProvider.Interface
{
    public interface IAdminProviderService
    {
        public ViewCaseViewModel? GetViewCaseModel(int requestId);
        public ViewNotesViewModel? GetViewNotesModel(int requestId);
        public ServiceResponse SubmitViewNotes(ViewNotesViewModel model, string aspNetUserId, bool isAdmin);
        public ViewUploadsViewModel? GetViewUploadsModel(int requestId);
        public ServiceResponse SubmitCreateRequest(AdminCreateRequestViewModel model, string adminAspId, string createAccLink, bool isAdmin);

    }
}
