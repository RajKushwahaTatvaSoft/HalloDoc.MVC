using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class SchedulingViewModel
    {
        public IEnumerable<Region>? regions { get; set; }
        public List<DataModels.Physician>? physicians { get; set; }
        public int? addShiftRegion { get; set; }
        public int? addShiftPhysician { get; set; }
        public DateTime? shiftDate { get; set; }
        public TimeOnly? shiftStartTime { get; set; }
        public TimeOnly? shiftEndTime { get; set; }
        public int? isRepeat { get; set; }
        public List<int>? repeatDays { get; set; }
        public int? repeatCount { get; set; }
        public List<string>? Hours { get; set; }
        public List<ShiftViewModel>? Shifts { get; set; }
        public string? ProviderName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
    }

    public class ShiftMonthViewModel
    {
        public IEnumerable<Shiftdetail> shiftDetails{ get; set; }

    }

    public class ShiftWeekViewModel
    {
        public DateTime StartOfWeek { get; set; }
        public DateTime EndOfWeek { get; set; }
        public IEnumerable<PhysicianShift> physicianShifts { get; set; }
        public IEnumerable<DataModels.Physician> physicians { get; set; }

    }

    public class PhysicianShift
    {
        public int PhysicianId { get; set; }
        public string PhysicianName {  get; set; }
        public int RegionId { get; set; }
        public IEnumerable<Shiftdetail> shiftDetails {  get; set; }
    }


    public class ShiftTableViewModel
    {
        public List<string>? Hours { get; set; }
        public List<string>? hourTime { get; set; }
        public List<ShiftViewModel>? Shifts { get; set; }
        public IEnumerable<DataModels.Physician> physicians { get; set; }

    }

    public class ShiftItem
    {
        public int PhysicianId { get; set; }
        public int RegionId {  get; set; }
        public string PhysicianName {  get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int Status {  get; set; }
        public DateTime ShiftDate { get; set; }
    }

    public class ShiftViewModel
    {
        public string? ProviderName { get; set; }
        public int PhysicianId { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public short Status { get; set; }
        public DateTime shiftDate { get; set; }
    }

}
