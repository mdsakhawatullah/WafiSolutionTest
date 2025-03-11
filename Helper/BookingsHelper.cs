using Microsoft.AspNetCore.Http.HttpResults;
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

            if (bookingDto.RepeatOption == RepeatOption.DoesNotRepeat)
            {
                bookings.Add(CreateBooking(bookingDto, bookingDto.BookingDate));
            }

            if (bookingDto.RepeatOption == RepeatOption.Daily && bookingDto.EndRepeatDate.HasValue)
            {

                DateOnly nextDate = bookingDto.BookingDate;

                while (nextDate <= bookingDto.EndRepeatDate.Value)
                {
                    bookings.Add(CreateBooking(bookingDto, nextDate));

                    nextDate = nextDate.AddDays(1);
                }
            }

             if (bookingDto.RepeatOption == RepeatOption.Weekly && bookingDto.DaysToRepeatOn.Any() && bookingDto.EndRepeatDate.HasValue)

            {
                DateOnly currentWeekStart = bookingDto.BookingDate; 

                while (currentWeekStart <= bookingDto.EndRepeatDate.Value)
                {
                    DateOnly nextDate = currentWeekStart;
                    int currentDayAsInt = ConvertToInt(currentWeekStart.DayOfWeek);

                    if (bookingDto.DaysToRepeatOn.Contains((DaysOfWeek)currentDayAsInt))
                    {
                        bookings.Add(CreateBooking(bookingDto, currentWeekStart));
                    }
                       currentWeekStart = currentWeekStart.AddDays(1);
                }
            }
            return bookings;

        }
        private static Booking CreateBooking(CreateUpdateBookingDto bookingDto, DateOnly bookingDate)
        {
            return new Booking
            {
                BookingId = Guid.NewGuid(),
                BookingDate = bookingDate,
                StartTime = bookingDto.StartTime,
                EndTime = bookingDto.EndTime,
                CarId = bookingDto.CarId,
                Note = bookingDto.Note,
                RepeatOption = bookingDto.RepeatOption,
                RequestedOn = DateTime.UtcNow,
                CreationTime = DateTime.UtcNow
            };
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
