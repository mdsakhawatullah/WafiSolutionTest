using System.ComponentModel.DataAnnotations;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Dtos
{
    public class BookingFilterByRepeatOptionsDto : BookingFilterDto
    {
        [Required(ErrorMessage = "Repeat Option is must")]
        public RepeatOption Type { get; set; }
    }
}
