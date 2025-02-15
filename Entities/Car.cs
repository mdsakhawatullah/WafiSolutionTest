using System.ComponentModel.DataAnnotations;

namespace Wafi.SampleTest.Entities
{
    public class Car
    {
        [Key]
        public Guid CarId { get; set; }

        public string? Brand { get; set; }

        [Required(ErrorMessage = "Car Model Name is required.")]
        public string Model { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

    }
}
