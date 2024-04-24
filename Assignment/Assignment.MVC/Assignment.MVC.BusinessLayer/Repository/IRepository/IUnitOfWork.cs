namespace Assignment.MVC.BusinessLayer.Repository.IRepository
{
    public interface IUnitOfWork
    {
        public IPatientRepository PatientRepository { get; }
        public IDoctorRepository DoctorRepository { get; }
        void Save();

    }
}