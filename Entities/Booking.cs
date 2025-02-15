using System.ComponentModel.DataAnnotations;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wafi.SampleTest.Entities
{
    public class Booking : CommonModel
    {
        [Key]
        public Guid BookingId { get; set; }

        [Required(ErrorMessage = "Booking Date is required")]
        public DateOnly BookingDate { get; set; }

        [Required(ErrorMessage = "Start Time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End Time is required")]
        [CustomValidation(typeof(Booking), nameof(ValidateTimeRange))]
        public TimeSpan EndTime { get; set; }

        public string? Note { get; set; }

        [Required(ErrorMessage = "Please select Repeat Option")]
        //Enum: DoesNotRepeat, Daily, Weekly
        public RepeatOption RepeatOption { get; set; }

        public DateOnly? EndRepeatDate { get; set; }

        //Enum: None,Sunday,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday
        public DaysOfWeek? DaysToRepeatOn { get; set; }

        public DateTime RequestedOn { get; set; }

        [BindNever]
        [ForeignKey("Car")]
        public Guid CarId { get; set; }

        //navigation property
        [BindNever]
        public Car Car { get; set; }

        public static ValidationResult ValidateTimeRange(TimeSpan endTime, ValidationContext context)
        {
            var instance = (Booking)context.ObjectInstance;
            if (instance.StartTime >= endTime)
            {
                return new ValidationResult("End Time must be after Start Time.");
            }
            return ValidationResult.Success;
        }
    }

    [Flags]
    #region Enum DaysofWeek
    public enum DaysOfWeek
    {
        None = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64
    }
    #endregion

    #region Enum RepeatOption
    public enum RepeatOption
    {
        DoesNotRepeat = 1,
        Daily = 2,
        Weekly = 3
    }
    #endregion
}
