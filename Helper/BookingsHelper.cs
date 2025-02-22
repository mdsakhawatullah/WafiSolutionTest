using System.ComponentModel.DataAnnotations;
using System.Linq;
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
            else if (bookingDto.RepeatOption == RepeatOption.Weekly && bookingDto.DaysToRepeatOn.Any() && bookingDto.EndRepeatDate.HasValue)
            {
                DateOnly currentWeekStart = bookingDto.BookingDate; 

                while (currentWeekStart <= bookingDto.EndRepeatDate.Value)
                {
                    DateOnly nextDate = currentWeekStart;
                    int currentDayAsInt = ConvertToInt(currentWeekStart.DayOfWeek);



                    if (bookingDto.DaysToRepeatOn.Contains((DaysOfWeek)currentDayAsInt))
                    {
                        bookings.Add(new Booking
                        {
                            BookingId = Guid.NewGuid(),
                            BookingDate = currentWeekStart,
                            StartTime = bookingDto.StartTime,
                            EndTime = bookingDto.EndTime,
                            CarId = bookingDto.CarId,
                            Note = bookingDto.Note,
                            RepeatOption = bookingDto.RepeatOption,
                            RequestedOn = DateTime.UtcNow
                        });
                    }
                    

                    currentWeekStart = currentWeekStart.AddDays(1); // Move to the next week
                }
            }
            return bookings;

        }

        private static int ConvertToInt(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Sunday => 1,
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 4,
                DayOfWeek.Wednesday => 8,
                DayOfWeek.Thursday => 16,
                DayOfWeek.Friday => 32,
                DayOfWeek.Saturday => 64,
                _ => 0
            };
        }


    }


}
