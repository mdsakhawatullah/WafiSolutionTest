namespace Wafi.SampleTest.Dtos
{
    public class CalenderViewDto
    {
        public DateOnly Date { get; set; }
        public List<BookingCalendarDto> Bookings { get; set; }
    }
}
