using System.ComponentModel.DataAnnotations;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Helper
{
    public class BookingsHelper
    {

        /// <summary>
        /// creating recurring bookings based on user input
        /// </summary>
        internal static List<Booking> GenerateRecurringBookings(CreateUpdateBookingDto bookingDto)
        {
            var bookings = new List<Booking>();

            //booking from user input
            if (bookingDto.RepeatOption == RepeatOption.DoesNotRepeat)
            {
                bookings.Add(new Booking
                {
                    BookingId = Guid.NewGuid(),
                    BookingDate = bookingDto.BookingDate,
                    StartTime = bookingDto.StartTime,
                    EndTime = bookingDto.EndTime,
                    CarId = bookingDto.CarId,
                    Note = bookingDto.Note,
                    RepeatOption = bookingDto.RepeatOption,
                    RequestedOn = DateTime.UtcNow,
                    CreationTime = DateTime.UtcNow

                });
            }


            // setup Daily Recurrence
            if (bookingDto.RepeatOption == RepeatOption.Daily && bookingDto.EndRepeatDate.HasValue)
            {
                DateOnly nextDate = bookingDto.BookingDate;

                while (nextDate <= bookingDto.EndRepeatDate.Value)
                {
                    bookings.Add(new Booking
                    {
                        BookingId = Guid.NewGuid(),
                        BookingDate = nextDate,
                        StartTime = bookingDto.StartTime,
                        EndTime = bookingDto.EndTime,
                        CarId = bookingDto.CarId,
                        Note = bookingDto.Note,
                        RepeatOption = bookingDto.RepeatOption,
                        RequestedOn = DateTime.UtcNow
                    });

                    nextDate = nextDate.AddDays(1);
                }
            }



            //setup Weekly Recurrence
            else if (bookingDto.RepeatOption == RepeatOption.Weekly && bookingDto.DaysToRepeatOn.HasValue && bookingDto.EndRepeatDate.HasValue)
            {
                DateOnly currentWeekStart = bookingDto.BookingDate; // Start from booking date

                while (currentWeekStart <= bookingDto.EndRepeatDate.Value)
                {
                    foreach (DaysOfWeek day in Enum.GetValues(typeof(DaysOfWeek)))
                    {
                        if (day != DaysOfWeek.None && bookingDto.DaysToRepeatOn.Value.HasFlag(day))
                        {
                            int daysToAdd = ((int)day - (int)currentWeekStart.DayOfWeek + 7) % 7;
                            DateOnly nextDate = currentWeekStart;

                            if (nextDate <= bookingDto.EndRepeatDate.Value)
                            {
                                bookings.Add(new Booking
                                {
                                    BookingId = Guid.NewGuid(),
                                    BookingDate = nextDate,
                                    StartTime = bookingDto.StartTime,
                                    EndTime = bookingDto.EndTime,
                                    CarId = bookingDto.CarId,
                                    Note = bookingDto.Note,
                                    RepeatOption = bookingDto.RepeatOption,
                                    RequestedOn = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    currentWeekStart = currentWeekStart.AddDays(7); // Move to the next week
                }
            }
            return bookings;
        }

    }
}
