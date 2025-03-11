using System.ComponentModel.DataAnnotations;

namespace Wafi.SampleTest.Dtos
{
    public class PaginationDto
    {
        [Required]
        public int pageNumber { get; set; }

        [Required]
        public int pageSize { get; set; }
    }
}
