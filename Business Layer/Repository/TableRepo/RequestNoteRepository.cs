using Business_Layer.Interface.TableInterface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.TableRepo
{
    public class RequestNoteRepository : Repository<Requestnote>, IRequestNoteRepository
    {
        private ApplicationDbContext _context;
        public RequestNoteRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
