using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Dtos
{
    public class CalenderViewRepeatOption
    {
        public RepeatOption Type { get; set; }
        public List<BookingCalendarDto> Bookings { get; set; }
    }
}
