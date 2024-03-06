using Data_Layer.CustomModels;
using Data_Layer.DataModels;

namespace HalloDoc.MVC.Services
{
    public class SessionUtils
    {
        public static SessionUser GetLoggedInUser(ISession session)
        {
            SessionUser user = null;
            if (session.GetInt32("userId") != null)
            {
                user = new SessionUser();
                user.UserId = (int) session.GetInt32("userId");
                user.Email = session.GetString("userEmail");
                user.UserName = session.GetString("userName");
                user.RoleId = (int) session.GetInt32("roleId");
            }

            return user;
        }

        public static void SetLoggedInUser(ISession session, SessionUser user)
        {
            if(user != null)
            {
                session.SetInt32("userId",user.UserId);
                session.SetString("userEmail",user.Email);
                session.SetString("userName",user.UserName);
                session.SetInt32("roleId",user.RoleId);
            }
        }
    }
}
