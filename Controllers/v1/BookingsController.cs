using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Globalization;
using Wafi.SampleTest.DbConfigure;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;
using Wafi.SampleTest.Helper;

namespace Wafi.SampleTest.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class BookingsController : ControllerBase
    {
        private readonly WafiDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(WafiDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// To get calenderView response
        /// </summary>
        /// <param name="bookingFilterDto"></param>
        /// <returns></returns>
        [HttpGet("calenderBookings")]
        public async Task<ActionResult<IEnumerable<CalenderViewDto>>> GetCalendarBookings([FromQuery] BookingFilterDto bookingFilterDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Invalid Request body");
                return BadRequest(ModelState);
            }

            try
            {

                bool carExists = await _context.Bookings.AnyAsync(b => b.CarId == bookingFilterDto.CarId);

                if (!carExists)
                    return NotFound("No record found for this car");

                var bookings = await _context.Bookings.Where(b => b.BookingDate >= bookingFilterDto.StartBookingDate
                                                                  && b.BookingDate <= bookingFilterDto.EndBookingDate
                                                                  && b.CarId == bookingFilterDto.CarId)
                                                      .Include(b => b.Car)
                                                      .ToListAsync();

                if (bookings.Count == 0)
                    return NotFound("No records found for selected date range");

                var groupedBookings = bookings
                                      .GroupBy(b => b.BookingDate)
                                      .Select(kvp => new CalenderViewDto
                                      {
                                          Date = kvp.Key,
                                          Bookings = kvp.Select(b => new BookingCalendarDto
                                          {
                                              BookingId = b.BookingId,
                                              BookingDate = b.BookingDate,
                                              StartTime = b.StartTime,
                                              EndTime = b.EndTime,
                                              CarDetails = new Car
                                              {
                                                  CarId = b.Car.CarId,
                                                  Brand = b.Car.Brand,
                                                  Model = b.Car.Model
                                              }
                                          }).ToList()
                                      }).ToList();

                return Ok(groupedBookings);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                return StatusCode(500, $"An unexpected error {ex.Message}");
            }
        }

        [HttpGet("calenderBookingsByRepeatOption")]
        public async Task<ActionResult<IEnumerable<CalenderViewRepeatOption>>> GetCalendarViewByRepeatOption([FromQuery] BookingFilterByRepeatOptionsDto repeatOptionFilterDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Invalid Request body");
                return BadRequest(ModelState);
            }
            try
            {
                if (!await _context.Bookings.AnyAsync(b => b.CarId == repeatOptionFilterDto.CarId))
                    return NotFound("No record found for this car");

                var bookings = await _context.Bookings.Where(b => b.BookingDate >= repeatOptionFilterDto.StartBookingDate
                                                                  && b.BookingDate <= repeatOptionFilterDto.EndBookingDate
                                                                  && b.CarId == repeatOptionFilterDto.CarId
                                                                  && b.RepeatOption == repeatOptionFilterDto.Type)
                                                      .Include(b => b.Car)
                                                      .ToListAsync();

                if (!bookings.Any())
                    return NotFound(new { Message = $"No {repeatOptionFilterDto.Type} bookings found." });

                var groupedBookings = bookings
            .GroupBy(b => b.RepeatOption)
            .Select(kvp => new CalenderViewRepeatOption
            {
                Type = kvp.Key,
                Bookings = kvp.Select(b => new BookingCalendarDto
                {
                    BookingId = b.BookingId,
                    BookingDate = b.BookingDate,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    CarDetails = new Car
                    {
                        CarId = b.Car.CarId,
                        Brand = b.Car.Brand,
                        Model = b.Car.Model
                    }
                }).ToList()
            }).ToList();

                return Ok(groupedBookings);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching calendar bookings: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while fetching calendar bookings." });
            }

        }


        //GET: api/Bookings/allbookings
        /// <summary>
        /// To get all bookings from the list
        /// </summary>
        /// <returns></returns>
        [HttpGet("AllBookings")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetAllBookings([FromQuery] PaginationDto pagination )
        {
            try
            {
                var bookings = await _context.Bookings
                    .Skip((pagination.pageNumber - 1) * pagination.pageSize)
                    .Take(pagination.pageSize)
                    .ToListAsync();

                if (!bookings.Any())
                {
                    _logger.LogInformation("No records found for bookings.");
                    return NotFound(new { Message = "No bookings available." });
                }

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving bookings: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while fetching bookings." });
            }

        }

        //GET: api/Bookings/BookingByCarId
        /// <summary>
        /// To get car Information by carId
        /// </summary>
        /// <param name="carId">GUID type carId</param>
        /// <returns></returns>
        [HttpGet("bookingByCarId/{carId}")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookingByCarId(Guid carId)
        {
            try
            {
                var bookingInformation = await _context.Bookings.Where(b => b.CarId == carId).ToListAsync();

                if (!bookingInformation.Any())
                {
                    _logger.LogInformation($"No Booking Information found for this carId: {carId}");
                    return NotFound($"No Booking Information found for this carId: {carId}");
                }

                return Ok(bookingInformation);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving for this Car Id {carId}: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching bookings.");
            }
        }


        // POST: api/Bookings
        /// <summary>
        /// To create booking
        /// </summary>
        /// <param name="bookingDto"></param>
        /// <returns></returns>
        [HttpPost("Booking")]
        public async Task<IActionResult> PostBooking(CreateUpdateBookingDto bookingDto)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Field Information is not correct");
                return BadRequest(ModelState);
            }

            try
            {
                
                if (bookingDto.StartTime >= bookingDto.EndTime)
                {
                    _logger.LogInformation("End time must be greater than start time.");
                    return BadRequest(new { Message = "End time must be greater than start time." });
                }

                bool isConflict = await _context.Bookings
                    .AnyAsync(b =>
                                    b.CarId == bookingDto.CarId &&
                                    b.BookingDate == bookingDto.BookingDate &&
                                    (bookingDto.StartTime >= b.StartTime && bookingDto.StartTime < b.EndTime ||
                                     bookingDto.EndTime > b.StartTime && bookingDto.EndTime <= b.EndTime ||
                                     b.StartTime >= bookingDto.StartTime && b.EndTime <= bookingDto.EndTime)
                            );

                if (isConflict)
                {
                    _logger.LogError("Booking conflict: A booking already exists for CarId {CarId} at this time.", bookingDto.CarId);
                    return Conflict(new { Message = "A booking already exists for this car at the selected time." });
                }

                if (bookingDto.EndRepeatDate <= bookingDto.BookingDate)
                    return BadRequest(new { Message = "EndRepeat Date must be greater than Booking Date" });

                var newBookings = BookingsHelper.GenerateRecurringBookings(bookingDto);

                if (!newBookings.Any())
                {
                    _logger.LogWarning("No valid bookings were generated.");
                    return BadRequest(new { Message = "No valid bookings were generated." });
                }

                await _context.Bookings.AddRangeAsync(newBookings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New booking created for CarId {CarId} at {BookingDate} - {StartTime}", bookingDto.CarId, bookingDto.BookingDate, bookingDto.StartTime);

                return CreatedAtAction(nameof(PostBooking), new { id = bookingDto.CarId }, bookingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occurred: {Error}", ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while processing the booking." });
            }
        }


        // GET: api/SeedData
        // For test purpose
        [HttpGet("SeedData")]
        public async Task<IEnumerable<BookingCalendarDto>> GetSeedData()
        {
            var cars = await _context.Cars.ToListAsync();

            if (!cars.Any())
            {
                cars = GetCars().ToList();
                await _context.Cars.AddRangeAsync(cars);
                await _context.SaveChangesAsync();
            }

            var bookings = await _context.Bookings.ToListAsync();

            if (!bookings.Any())
            {
                bookings = GetBookings().ToList();

                await _context.Bookings.AddRangeAsync(bookings);
                await _context.SaveChangesAsync();
            }

            var calendar = new Dictionary<DateOnly, List<Booking>>();

            foreach (var booking in bookings)
            {
                var currentDate = booking.BookingDate;
                while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate))
                {
                    if (!calendar.ContainsKey(currentDate))
                        calendar[currentDate] = new List<Booking>();

                    calendar[currentDate].Add(booking);

                    currentDate = booking.RepeatOption switch
                    {
                        RepeatOption.Daily => currentDate.AddDays(1),
                        RepeatOption.Weekly => currentDate.AddDays(7),
                        _ => booking.EndRepeatDate.HasValue ? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
                    };
                }
            }

            List<BookingCalendarDto> result = new List<BookingCalendarDto>();

            foreach (var item in calendar)
            {
                foreach (var booking in item.Value)
                {
                    result.Add(new BookingCalendarDto { BookingDate = booking.BookingDate, StartTime = booking.StartTime, EndTime = booking.EndTime });
                }
            }

            return result;
        }

        #region Sample Data

        private IList<Car> GetCars()
        {
            var cars = new List<Car>
            {
                new Car { CarId = Guid.NewGuid(), Brand = "Toyota", Model = "Corolla" },
                new Car { CarId = Guid.NewGuid(), Brand = "Honda", Model = "Civic" },
                new Car { CarId = Guid.NewGuid(), Brand = "Ford", Model = "Focus" }
            };

            return cars;
        }

        private IList<Booking> GetBookings()
        {
            var cars = GetCars();

            var bookings = new List<Booking>
            {
                new Booking { BookingId = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 5), StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0), RepeatOption = RepeatOption.DoesNotRepeat, RequestedOn = DateTime.Now, CarId = cars[0].CarId, Car = cars[0] },
                new Booking { BookingId = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 10), StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 0, 0), RepeatOption = RepeatOption.Daily, EndRepeatDate = new DateOnly(2025, 2, 20), RequestedOn = DateTime.Now, CarId = cars[1].CarId, Car = cars[1] },
                new Booking { BookingId = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 15), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), RepeatOption = RepeatOption.Weekly, EndRepeatDate = new DateOnly(2025, 3, 31), RequestedOn = DateTime.Now, DaysToRepeatOn = DaysOfWeek.Monday, CarId = cars[2].CarId,  Car = cars[2] },
                new Booking { BookingId = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 1), StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(13, 0, 0), RepeatOption = RepeatOption.DoesNotRepeat, RequestedOn = DateTime.Now, CarId = cars[0].CarId, Car = cars[0] },
                new Booking { BookingId = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 7), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(10, 0, 0), RepeatOption = RepeatOption.Weekly, EndRepeatDate = new DateOnly(2025, 3, 28), RequestedOn = DateTime.Now, DaysToRepeatOn = DaysOfWeek.Friday, CarId = cars[1].CarId, Car = cars[1] },
                new Booking { BookingId = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 15), StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(17, 0, 0), RepeatOption = RepeatOption.Daily, EndRepeatDate = new DateOnly(2025, 3, 20), RequestedOn = DateTime.Now, CarId = cars[2].CarId,  Car = cars[2] }
            };

            return bookings;
        }

        #endregion

    }
}
