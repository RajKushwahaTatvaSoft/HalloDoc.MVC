using Business_Layer.Utilities;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.SignalR;
using System.Text;

namespace HalloDoc.MVC.ChatService
{
    public class ChatHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        public ChatHub(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendMessage(string receiverAspUserId, string message, string requestId)
        {

            string? senderConnectionId = Context.ConnectionId;
            string? senderAspUserId = _context.UserConnections.Where(x => x.SignalConnectionId == senderConnectionId).Select(x => x.UserAspNetUserId).FirstOrDefault();
            int requestIdNumber = Convert.ToInt32(requestId);

            if (senderAspUserId == null)
            {
                return;
            }

            string? groupName = GetGroupNameForChat(receiverAspUserId, senderAspUserId, requestIdNumber);

            if (groupName == null)
            {
                return;
            }
            ChatMessage chatMessage = new()
            {
                SenderAspId = senderAspUserId,
                ReceiverAspId = receiverAspUserId,
                MessageContent = message,
                SentTime = DateTime.Now,
                RequestId = requestIdNumber,
            };

            _context.ChatMessages.Add(chatMessage);
            _context.SaveChanges();

            string? receiverConnectionId = _context.UserConnections.Where(x => x.UserAspNetUserId == receiverAspUserId).Select(x => x.SignalConnectionId).FirstOrDefault();

            if (receiverConnectionId != null)
            {
                await Groups.AddToGroupAsync(senderConnectionId, groupName);
                await Groups.AddToGroupAsync(receiverConnectionId, groupName);
                await Clients.Group(groupName).SendAsync("ReceiveMessage", senderAspUserId, message);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "You", message);
                //await Clients.Caller.SendAsync("ReceiverNotAvailable", receiverAspUserId);
            }

        }

        public async Task SendMessageToGroup(string message, string requestId)
        {

            string? senderConnectionId = Context.ConnectionId;
            string? senderAspUserId = _context.UserConnections.Where(x => x.SignalConnectionId == senderConnectionId).Select(x => x.UserAspNetUserId).FirstOrDefault();
            int requestIdNumber = Convert.ToInt32(requestId);

            if (senderAspUserId == null)
            {
                return;
            }

            string? groupName = $"GroupForRequest{requestId}";

            //ChatMessage chatMessage = new()
            //{
            //    SenderAspId = senderAspUserId,
            //    ReceiverAspId = groupName,
            //    MessageContent = message,
            //    SentTime = DateTime.Now,
            //    RequestId = requestIdNumber,
            //};

            //_context.ChatMessages.Add(chatMessage);
            //_context.SaveChanges();

            List<string> groupMemberConnectionIds = getGroupMembersConnectionIds(requestIdNumber);

            foreach (string connectionId in groupMemberConnectionIds)
            {
                await Groups.AddToGroupAsync(connectionId, groupName);
            }

            int accountTypeId = _context.Aspnetusers.FirstOrDefault(user => user.Id == senderAspUserId)?.Accounttypeid ?? 0;

            string imagePath = "/images//default/group_default_svg.svg";

            switch ((AccountType) accountTypeId)
            {
                case AccountType.Admin:
                    {
                        imagePath = "/images//default/admin_default_svg.svg";
                        break;
                    }

                case AccountType.Physician:
                    {
                        imagePath = "/images//default/physician_default_svg.svg";
                        break;
                    }

                case AccountType.Patient:
                    {
                        imagePath = "/images//default/patient_default_svg.svg";
                        break;
                    }
            }


            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", senderAspUserId, message, imagePath);

        }

        private List<string> getGroupMembersConnectionIds(int requestId)
        {
            List<string> response = new List<string>();
            Request? request = _context.Requests.FirstOrDefault(req => req.Requestid == requestId);

            if (request == null)
            {
                return response;
            }

            string adminAspId = Constants.MASTER_ADMIN_ASP_USER_ID;
            string? phyAspId = _context.Physicians.FirstOrDefault(phy => phy.Physicianid == request.Physicianid)?.Aspnetuserid;
            string? userAspId = _context.Users.FirstOrDefault(user => user.Userid == request.Userid)?.Aspnetuserid;

            string? adminConnectionId = _context.UserConnections.FirstOrDefault(con => con.UserAspNetUserId == adminAspId)?.SignalConnectionId;
            string? phyConnectionId = _context.UserConnections.FirstOrDefault(con => con.UserAspNetUserId == phyAspId)?.SignalConnectionId;
            string? userConnectionId = _context.UserConnections.FirstOrDefault(con => con.UserAspNetUserId == userAspId)?.SignalConnectionId;

            response.Add(adminConnectionId ?? "");
            response.Add(phyConnectionId ?? "");
            response.Add(userConnectionId ?? "");

            return response;
        }

        private string? GetGroupNameForChat(string receiverAspUserId, string senderAspUserId, int requestId)
        {
            int receiverAccountType = _context.Aspnetusers.FirstOrDefault(user => user.Id == receiverAspUserId)?.Accounttypeid ?? 0;
            int senderAccountType = _context.Aspnetusers.FirstOrDefault(user => user.Id == senderAspUserId)?.Accounttypeid ?? 0;

            if (receiverAccountType == 0 || senderAccountType == 0 || requestId == 0)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder();

            if (receiverAccountType == (int)AccountType.Admin || senderAccountType == (int)AccountType.Admin)
            {
                stringBuilder.Append("Admin");
            }

            if (receiverAccountType == (int)AccountType.Physician || senderAccountType == (int)AccountType.Physician)
            {
                stringBuilder.Append("Phy");
            }

            if (receiverAccountType == (int)AccountType.Patient || senderAccountType == (int)AccountType.Patient)
            {
                stringBuilder.Append("Patient");
            }

            stringBuilder.Append($"Request{requestId}");

            return stringBuilder.ToString();
        }

        public override Task OnConnectedAsync()
        {
            string? aspnetID = _httpContextAccessor.HttpContext!.Session.GetString("userAspId");

            if (aspnetID != null)
            {
                UserConnection? connectedUserId = _context.UserConnections.Where(x => x.UserAspNetUserId == aspnetID).FirstOrDefault();
                if (connectedUserId == null)
                {
                    UserConnection userConnection = new UserConnection();
                    userConnection.SignalConnectionId = Context.ConnectionId;
                    userConnection.UserAspNetUserId = aspnetID;
                    _context.UserConnections.Add(userConnection);
                    _context.SaveChanges();
                }
                else
                {
                    connectedUserId.SignalConnectionId = Context.ConnectionId;
                    _context.UserConnections.Update(connectedUserId);
                    _context.SaveChanges();
                }
            }
            else
            {
                Console.WriteLine("Warning: UserId is null on connection.");
            }
            return base.OnConnectedAsync();
        }

    }
}