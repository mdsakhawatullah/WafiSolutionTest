using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Helper
{
    public class AddDictionaryHelper
    {

        /// <summary>
        /// adding Booking entry into the Dictionary
        /// </summary>
        /// <param name="groupedBookings">contains Dictionary type</param>
        /// <param name="booking">contains booking related information</param>
        internal static void AddBookingToDictionary(Dictionary<DateOnly, List<BookingCalendarDto>> groupedBookings, Booking booking, DateOnly date)
        {
            if (!groupedBookings.ContainsKey(date))
            {
                groupedBookings[date] = new List<BookingCalendarDto>();
            }


            groupedBookings[date].Add(new BookingCalendarDto
            {
                BookingId = booking.BookingId,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                CarDetails = new Car
                {
                    CarId = booking.Car.CarId,
                    Brand = booking.Car.Brand,
                    Model = booking.Car.Model

                }
            });

        }
    }
}
