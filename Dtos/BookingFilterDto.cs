using System.ComponentModel.DataAnnotations;

namespace Wafi.SampleTest.Dtos
{
    public class BookingFilterDto
    {
        [Required(ErrorMessage = "CarId is blank")]
        public Guid CarId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateOnly StartBookingDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateOnly EndBookingDate { get; set; }
    }
}
