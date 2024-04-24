using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminProvider.Interface;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminProvider
{
    internal class AdminProviderService : IAdminProviderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly IEmailService _emailService;
        public AdminProviderService(IUnitOfWork unitOfWork, IUtilityService utilityService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _emailService = emailService;
        }

        public ViewCaseViewModel? GetViewCaseModel(int requestId)
        {

            Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
            Requestclient? client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqCli => reqCli.Requestid == requestId);

            if (req == null || client == null)
            {
                return null;
            }

            ViewCaseViewModel model = new();

            model.RequestId = requestId;

            if (client.Intyear != null && client.Intdate != null && client.Strmonth != null)
            {
                model.Dob = DateHelper.GetDOBDateTime(client.Intyear ?? 0, client.Strmonth, client.Intdate ?? 0);
            }

            model.Confirmation = req.Confirmationnumber;
            model.DashboardStatus = RequestHelper.GetDashboardStatus(req.Status);
            model.RequestType = req.Requesttypeid;
            model.PatientName = client.Firstname + " " + client.Lastname;
            model.PatientFirstName = client.Firstname;
            model.PatientLastName = client.Lastname;
            model.PatientEmail = client.Email;
            model.Region = client.Regionid;
            model.Notes = client.Notes;
            model.Address = client.Street;
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.physicians = _unitOfWork.PhysicianRepository.GetAll();
            model.casetags = _unitOfWork.CaseTagRepository.GetAll();

            return model;
        }

        public ViewNotesViewModel? GetViewNotesModel(int requestId)
        {
            Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
            if (request == null)
            {
                return null;
            }

            IEnumerable<Requeststatuslog> statusLogs = (from log in _unitOfWork.RequestStatusLogRepository.GetAll()
                                                        where log.Requestid == requestId
                                                        && log.Status != (int)RequestStatus.Cancelled
                                                        && log.Status != (int)RequestStatus.CancelledByPatient
                                                        select log
                                                        ).OrderByDescending(_ => _.Createddate);

            Requestnote? notes = _unitOfWork.RequestNoteRepository.GetFirstOrDefault(notes => notes.Requestid == requestId);

            string? cancelledByAdmin = _unitOfWork.RequestStatusLogRepository.GetFirstOrDefault(log => log.Status == (int)RequestStatus.Cancelled)?.Notes;
            string? cancelledByPatient = _unitOfWork.RequestStatusLogRepository.GetFirstOrDefault(log => log.Status == (int)RequestStatus.CancelledByPatient)?.Notes;

            ViewNotesViewModel model = new ViewNotesViewModel();

            model.requeststatuslogs = statusLogs;

            if (notes != null)
            {
                model.AdminNotes = notes.Adminnotes;
                model.PhysicianNotes = notes.Physiciannotes;
                model.AdminCancellationNotes = cancelledByAdmin;
                model.PatientCancellationNotes = cancelledByPatient;
            }

            return model;
        }

        public ServiceResponse SubmitViewNotes(ViewNotesViewModel model, string aspNetUserId, bool isAdmin)
        {

            try
            {
                Requestnote? oldnote = _unitOfWork.RequestNoteRepository.GetFirstOrDefault(rn => rn.Requestid == model.RequestId);

                if (oldnote != null)
                {
                    oldnote.Modifieddate = DateTime.Now;
                    oldnote.Modifiedby = aspNetUserId;
                    if (isAdmin)
                    {
                        oldnote.Adminnotes = model.InputNotes;
                    }
                    else
                    {
                        oldnote.Physiciannotes = model.InputNotes;
                    }

                    _unitOfWork.RequestNoteRepository.Update(oldnote);
                    _unitOfWork.Save();

                }
                else
                {
                    Requestnote curReqNote = new Requestnote()
                    {
                        Requestid = model.RequestId,
                        Createdby = aspNetUserId,
                        Createddate = DateTime.Now,
                    };

                    if (isAdmin)
                    {
                        curReqNote.Adminnotes = model.InputNotes;
                    }
                    else
                    {
                        curReqNote.Physiciannotes = model.InputNotes;
                    }

                    _unitOfWork.RequestNoteRepository.Add(curReqNote);
                    _unitOfWork.Save();

                }

                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Success,
                };

            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = ex.Message,
                };
            }

        }

        public ViewUploadsViewModel? GetViewUploadsModel(int requestId, bool isAdmin)
        {

            Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
            if (req == null)
            {
                return null;
            }

            Requestclient? reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == req.Requestid);
            if (reqCli == null)
            {
                return null;
            }

            List<Requestwisefile> files = _unitOfWork.RequestWiseFileRepository.Where(file => file.Requestid == requestId && file.Isdeleted != true).ToList();

            List<string> ext = new List<string>();
            for (int i = 0; i < files.Count; i++)
            {
                ext.Add(Path.GetExtension(files[i].Filename));
            }

            ViewUploadsViewModel model = new ViewUploadsViewModel()
            {
                IsAdmin = isAdmin,
                PatientName = reqCli.Firstname + " " + reqCli.Lastname,
                requestwisefiles = files,
                ConfirmationNumber = req.Confirmationnumber,
                RequestId = req.Requestid,
                extensions = ext,
            };

            return model;
        }

        public ServiceResponse SubmitCreateRequest(AdminCreateRequestViewModel model, string aspNetUserId, string createAccLink, bool isAdmin)
        {
            ServiceResponse response = new ServiceResponse();
            Data_Layer.DataModels.Physician? phy = new Data_Layer.DataModels.Physician();
            if (!isAdmin)
            {
                phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Aspnetuserid == aspNetUserId);

                if (phy == null)
                {
                    response.StatusCode = ResponseCode.Error;
                    response.Message = "Physician not found";
                    return response;
                }

            }

            try
            {

                string phoneNumber = "+" + model.CountryCode + "-" + model.PhoneNumber;
                string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(reg => reg.Regionid == model.RegionId)?.Name;
                string? city = _unitOfWork.CityRepository.GetFirstOrDefault(reg => reg.Id == model.CityId)?.Name;

                // Creating Patient in Aspnetusers Table

                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(model.Email);

                if (!isUserExists)
                {

                    Guid generatedId = Guid.NewGuid();

                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = _utilityService.GenerateUserName(AccountType.Patient,model.FirstName,model.LastName),
                        Passwordhash = null,
                        Email = model.Email,
                        Phonenumber = phoneNumber,
                        Createddate = DateTime.Now,
                        Accounttypeid = (int)AccountType.Patient,
                    };

                    _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                    _unitOfWork.Save();

                    User user1 = new()
                    {
                        Aspnetuserid = generatedId.ToString(),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Email = model.Email,
                        Mobile = phoneNumber,
                        Street = model.Street,
                        City = city,
                        State = state,
                        Regionid = model.RegionId,
                        Zipcode = model.ZipCode,
                        Createddate = DateTime.Now,
                        Createdby = aspNetUserId,
                        Strmonth = model.DOB?.Month.ToString(),
                        Intdate = model.DOB?.Day,
                        Intyear = model.DOB?.Year
                    };


                    _unitOfWork.UserRepository.Add(user1);
                    _unitOfWork.Save();

                    _emailService.SendMailForCreateAccount(model.Email, user1.Aspnetuserid, createAccLink);
                }

                User? user = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Email == model.Email);

                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Patient,
                    Userid = user.Userid,
                    Firstname = model.FirstName,
                    Lastname = model.LastName,
                    Phonenumber = phoneNumber,
                    Email = model.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                    Patientaccountid = user.Aspnetuserid,
                };

                if (!isAdmin)
                {
                    request.Status = (short)RequestStatus.Accepted;
                    request.Physicianid = phy.Physicianid;
                }

                _unitOfWork.RequestRepository.Add(request);
                _unitOfWork.Save();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = model.FirstName,
                    Lastname = model.LastName,
                    Phonenumber = phoneNumber,
                    Email = model.Email,
                    Address = model.Street,
                    City = city,
                    State = state,
                    Regionid = model.RegionId,
                    Zipcode = model.ZipCode,
                    Strmonth = model.DOB?.Month.ToString(),
                    Intdate = model.DOB?.Day,
                    Intyear = model.DOB?.Year
                };

                _unitOfWork.RequestClientRepository.Add(requestclient);
                _unitOfWork.Save();
                if (model.Notes != null)
                {
                    Requestnote rn = new()
                    {
                        Requestid = request.Requestid,
                        Createdby = aspNetUserId,
                        Createddate = DateTime.Now
                    };
                    if (isAdmin)
                    {
                        rn.Adminnotes = model.Notes;
                    }
                    else
                    {
                        rn.Physiciannotes = model.Notes;
                    }

                    _unitOfWork.RequestNoteRepository.Add(rn);
                    _unitOfWork.Save();
                }

                model.regions = _unitOfWork.RegionRepository.GetAll();

                response.StatusCode = ResponseCode.Success;
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.Error;
                response.Message = ex.Message;
                return response;
            }

        }

        public ServiceResponse SubmitOrderDetails(SendOrderViewModel model, string aspUserId)
        {

            Orderdetail order = new Orderdetail()
            {
                Vendorid = model.SelectedVendor,
                Requestid = model.RequestId,
                Faxnumber = model.FaxNumber,
                Email = model.Email,
                Businesscontact = model.BusinessContact,
                Prescription = model.Prescription,
                Noofrefill = model.NoOfRefills,
                Createddate = DateTime.Now,
                Createdby = aspUserId,
            };

            _unitOfWork.OrderDetailRepo.Add(order);
            _unitOfWork.Save();
            return new ServiceResponse
            {
                StatusCode = ResponseCode.Success,
            };

        }

        public EncounterFormViewModel? GetEncounterFormModel(int requestId, bool isAdmin)
        {
            Encounterform? encounterform = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(e => e.Requestid == requestId);
            if (!isAdmin && encounterform != null && encounterform.Isfinalize)
            {
                return null;
            }

            Requestclient? requestclient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(e => e.Requestid == requestId);
            Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(r => r.Requestid == requestId);
            DateTime? dobDate = null;

            if (request == null || requestclient == null)
            {
                return null;
            }

            if (requestclient.Intyear != null && requestclient.Strmonth != null && requestclient.Intdate != null)
            {
                dobDate = DateHelper.GetDOBDateTime(requestclient.Intyear ?? 0, requestclient.Strmonth, requestclient.Intdate ?? 0);
            }

            EncounterFormViewModel model = new()
            {
                RequestId = requestId,
                IsAdmin = isAdmin,
                FirstName = requestclient.Firstname,
                LastName = requestclient.Lastname,
                Email = requestclient.Email,
                PhoneNumber = requestclient.Phonenumber,
                DOB = dobDate,
                CreatedDate = request.Createddate,
                Location = requestclient.Street + " " + requestclient.City + " " + requestclient.State,
                MedicalHistory = encounterform?.Medicalhistory,
                History = encounterform?.Historyofpresentillnessorinjury,
                Medications = encounterform?.Medications,
                Allergies = encounterform?.Allergies,
                Temp = encounterform?.Temp,
                HR = encounterform?.Hr,
                RR = encounterform?.Rr,
                BpLow = encounterform?.Bloodpressuresystolic,
                BpHigh = encounterform?.Bloodpressuresystolic,
                O2 = encounterform?.O2,
                Pain = encounterform?.Pain,
                Heent = encounterform?.Heent,
                CV = encounterform?.Cv,
                Chest = encounterform?.Chest,
                ABD = encounterform?.Abd,
                Extr = encounterform?.Extremities,
                Skin = encounterform?.Skin,
                Neuro = encounterform?.Neuro,
                Other = encounterform?.Other,
                Diagnosis = encounterform?.Diagnosis,
                TreatmentPlan = encounterform?.TreatmentPlan,
                Procedures = encounterform?.Procedures,
                MedicationDispensed = encounterform?.Medicaldispensed,
                FollowUps = encounterform?.Followup,
            };

            return model;
        }
        public ServiceResponse SubmitEncounterForm(EncounterFormViewModel model, bool isAdmin, int userId)
        {
            ServiceResponse response;

            Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(rs => rs.Requestid == model.RequestId);

            if (request == null)
            {
                response = new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = "Invalid Request",
                };

                return response;
            }


            Encounterform? encounterform = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(e => e.Requestid == model.RequestId);
            string phoneNumber = "+" + model.CountryCode + "-" + model.PhoneNumber;
            if (encounterform == null)
            {
                Encounterform encf = new()
                {
                    Requestid = model.RequestId,
                    Historyofpresentillnessorinjury = model.History,
                    Medicalhistory = model.MedicalHistory,
                    Medications = model.Medications,
                    Allergies = model.Allergies,
                    Temp = model.Temp,
                    Hr = model.HR,
                    Rr = model.RR,
                    Bloodpressuresystolic = model.BpLow,
                    Bloodpressurediastolic = model.BpHigh,
                    O2 = model.O2,
                    Pain = model.Pain,
                    Skin = model.Skin,
                    Heent = model.Heent,
                    Neuro = model.Neuro,
                    Other = model.Other,
                    Cv = model.CV,
                    Chest = model.Chest,
                    Abd = model.ABD,
                    Extremities = model.Extr,
                    Diagnosis = model.Diagnosis,
                    TreatmentPlan = model.TreatmentPlan,
                    Procedures = model.Procedures,
                    Adminid = isAdmin ? userId : null,
                    Physicianid = isAdmin ? null : userId,
                    Isfinalize = false
                };
                _unitOfWork.EncounterFormRepository.Add(encf);
                _unitOfWork.Save();
            }
            else
            {
                encounterform.Requestid = model.RequestId;
                encounterform.Historyofpresentillnessorinjury = model.History;
                encounterform.Medicalhistory = model.MedicalHistory;
                encounterform.Medications = model.Medications;
                encounterform.Allergies = model.Allergies;
                encounterform.Temp = model.Temp;
                encounterform.Hr = model.HR;
                encounterform.Rr = model.RR;
                encounterform.Bloodpressuresystolic = model.BpLow;
                encounterform.Bloodpressurediastolic = model.BpHigh;
                encounterform.O2 = model.O2;
                encounterform.Pain = model.Pain;
                encounterform.Skin = model.Skin;
                encounterform.Heent = model.Heent;
                encounterform.Neuro = model.Neuro;
                encounterform.Other = model.Other;
                encounterform.Cv = model.CV;
                encounterform.Chest = model.Chest;
                encounterform.Abd = model.ABD;
                encounterform.Extremities = model.Extr;
                encounterform.Diagnosis = model.Diagnosis;
                encounterform.TreatmentPlan = model.TreatmentPlan;
                encounterform.Procedures = model.Procedures;
                encounterform.Adminid = isAdmin ? userId : null;
                encounterform.Physicianid = isAdmin ? null : userId;

                _unitOfWork.EncounterFormRepository.Update(encounterform);
                _unitOfWork.Save();
            }

            response = new ServiceResponse
            {
                StatusCode = ResponseCode.Success,
            };

            return response;

        }
    }
}
