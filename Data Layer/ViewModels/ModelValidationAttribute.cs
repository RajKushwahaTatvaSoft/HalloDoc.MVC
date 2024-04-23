using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class AfterCurrentTime : ValidationAttribute
    {
        private readonly string _shiftDatePropertyName;

        public AfterCurrentTime(string startPropertyName)
        {
            _shiftDatePropertyName = startPropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            TimeOnly? startTime = (TimeOnly?)value;
            var shiftDateProperty = validationContext.ObjectType.GetProperty(_shiftDatePropertyName);
            if (shiftDateProperty == null)
            {
                return new ValidationResult("Shift date is not a property");
            }

            DateTime? shiftDate = (DateTime?)shiftDateProperty.GetValue(validationContext.ObjectInstance);

            if (startTime == null || shiftDate == null)
            {
                return new ValidationResult("Please fill all details");
            }

            DateTime? shiftTime = shiftDate?.Add(startTime.Value.ToTimeSpan());

            if (shiftTime <= DateTime.Now)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }

    }

    public class EndAfterStart : ValidationAttribute
    {
        private readonly string _startTimePropertyName;

        public EndAfterStart(string startPropertyName)
        {
            _startTimePropertyName = startPropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var endTime = (TimeOnly?)value;
            var startTimeProperty = validationContext.ObjectType.GetProperty(_startTimePropertyName);
            if (startTimeProperty == null)
            {
                return new ValidationResult("start time is not a property");
            }

            var startTime = (TimeOnly?)startTimeProperty.GetValue(validationContext.ObjectInstance);

            if (endTime == null || startTime == null || startTime >= endTime)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }

    }

    public class DateNotInFuture : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            DateTime? date = (DateTime?)value;
            if (date > DateTime.Now)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }

    public class DateNotInPast: ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            DateTime? date = (DateTime?)value;
            if (date < DateTime.Now.Date)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}
