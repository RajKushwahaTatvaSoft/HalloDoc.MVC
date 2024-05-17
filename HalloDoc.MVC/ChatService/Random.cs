using Microsoft.AspNetCore.SignalR;
using Data_Layer.DataContext;
using Data_Layer.DataModels;


namespace HalloDoc.Chat
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

        public async Task SendMessage(string receiverAspUserId, string message)
        {
            string? senderConnectionId = Context.ConnectionId;
            string? senderAspUserId = _context.UserConnections
                .Where(x => x.SignalConnectionId == senderConnectionId)
                .Select(x => x.UserAspNetUserId)
                .FirstOrDefault();

            string? receiverConnectionId = _context.UserConnections
                .Where(x => x.UserAspNetUserId == receiverAspUserId)
                .Select(x => x.SignalConnectionId)
                .FirstOrDefault();

            if (receiverConnectionId != null)
            {
                // Send the message to the receiver in real-time
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", senderAspUserId, message);
            }
            else
            {
                // Notify sender that the receiver is not available
                await Clients.Caller.SendAsync("ReceiverNotAvailable", receiverAspUserId);
            }

            // Send the message to the sender as well
            await Clients.Client(senderConnectionId).SendAsync("ReceiveMessage", "You", message);
        }

    }
}

