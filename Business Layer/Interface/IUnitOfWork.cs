using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Interface
{
    public interface IUnitOfWork
    {
        IAspNetUserRepository AspNetUserRepository { get; }
        IUserRepository UserRepository { get; }
        IRequestRepository RequestRepository { get; }
        IRequestClientRepository RequestClientRepository { get; }
        IRequestWiseFileRepository RequestWiseFileRepository { get; }
        IConciergeRepository ConciergeRepository { get; }
        IRequestConciergeRepo RequestConciergeRepository { get; }
        IBusinessRepo BusinessRepo { get; }
        IRequestBusinessRepo RequestBusinessRepo { get; }
        ICaseTagRepository CaseTagRepository { get; }
        IRequestStatusLogRepo RequestStatusLogRepository { get; }
        IPassTokenRepository PassTokenRepository { get; }
        void Save();
    }
}