using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Globalization;
using Wafi.SampleTest.DbConfigure;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly WafiDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(WafiDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }



        // GET: api/Bookings/Booking
        [HttpGet("calenderBookings")]
        public async Task<ActionResult<IEnumerable<CalenderViewDto>>> GetCalendarBookings([FromQuery]BookingFilterDto bookingfilterDto)
        {
            var bookings = await _context.Bookings
              .Where(b => b.BookingDate >= bookingfilterDto.StartBookingDate && b.BookingDate <= bookingfilterDto.EndBookingDate)
              .Include(b => b.Car)
              .ToListAsync();

            var groupedBookings = new Dictionary<DateOnly, List<BookingCalendarDto>>();

            foreach(var booking in bookings)
            {
             #region Handle Non-recurring Bookings
             if(booking.RepeatOption == RepeatOption.DoesNotRepeat)
                {
                    AddBookingToDictionary(groupedBookings, booking, booking.BookingDate);

                }
                #endregion


                #region Handle Daily Recurring

                
                #endregion
            }

            var calenderResponse = groupedBookings.Select(kvp => new CalenderViewDto
            {
                Date = kvp.Key,
                Bookings = kvp.Value
            });

            return Ok(calenderResponse);





            // Get booking from the database and filter the data
            //var bookings = await _context.Bookings.ToListAsync();

            // TO DO: convert the database bookings to calendar view (date, start time, end time). Consiser NoRepeat, Daily and Weekly options
            //return bookings;


        }


        //GET: api/Bookings/allbookings
        [HttpGet("AllBookings")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetAllBookings()
        {
            return await _context.Bookings.ToListAsync();
        }

        //GET: api/Bookings/BookingByCarId
        [HttpGet("bookingByCarId/{carId}")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookingByCarId(Guid carId)
        {
            var bookingInformation = await _context.Bookings.Where(b => b.CarId == carId).ToListAsync();

            if(!bookingInformation.Any())
            {
                _logger.LogInformation("No Booking Information found for this carId");
                return NotFound(new {Message = $"No Booking Information found for this carId: {carId}"});
            }

            return bookingInformation;
        }


        // POST: api/Bookings
        [HttpPost("Booking")]
        public async Task<IActionResult> PostBooking(CreateUpdateBookingDto bookingDto)
        {

            //validate input model
            if(!ModelState.IsValid)
            {
                _logger.LogInformation("Field Information is not correct");
                return BadRequest(ModelState);
            }

            //  Validate time range
            if (bookingDto.StartTime >= bookingDto.EndTime)
            {
                _logger.LogInformation("End time must be greater than start time.");
                return BadRequest(new { Message = "End time must be greater than start time." });
            }

            //check for duplicate booking
            bool isConflict = await _context.Bookings
                .AnyAsync(b =>
                                b.CarId == bookingDto.CarId &&
                                b.BookingDate == bookingDto.BookingDate &&
                                ((bookingDto.StartTime >= b.StartTime && bookingDto.StartTime < b.EndTime) ||
                                 (bookingDto.EndTime > b.StartTime && bookingDto.EndTime <= b.EndTime) ||
                                 (b.StartTime >= bookingDto.StartTime && b.EndTime <= bookingDto.EndTime))
                        );

            // check if duplicate booking is true
            if (isConflict)
            {
                _logger.LogError("Booking conflict: A booking already exists for CarId {CarId} at this time.", bookingDto.CarId);
                return Conflict(new { Message = "A booking already exists for this car at the selected time." });
            }

            //setup recurrence
            var newBookings = GenerateRecurringBookings(bookingDto);

            //save bookings to the database
            await _context.Bookings.AddRangeAsync(newBookings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New booking created for CarId {CarId} at {BookingDate} - {StartTime}", bookingDto.CarId, bookingDto.BookingDate, bookingDto.StartTime);

            return CreatedAtAction(nameof(PostBooking), new { id = bookingDto.CarId }, bookingDto);

        }

        /// <summary>
        /// creating recurring bookings based on user input
        /// </summary>
        private List<Booking> GenerateRecurringBookings (CreateUpdateBookingDto bookingDto)
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
                DateOnly nextDate = bookingDto.BookingDate.AddDays(1);

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
            else if (bookingDto.RepeatOption == RepeatOption.Weekly && bookingDto.DaysToRepeatOn.HasValue && bookingDto.EndRepeatDate.HasValue)
            {
                DateOnly currentWeekStart = bookingDto.BookingDate; // Start from booking date

                while (currentWeekStart <= bookingDto.EndRepeatDate.Value)
                {
                    foreach (DaysOfWeek day in Enum.GetValues(typeof(DaysOfWeek)))
                    {
                        if (day != DaysOfWeek.None && bookingDto.DaysToRepeatOn.Value.HasFlag(day))
                        {
                            int daysToAdd = ((int)day - (int)currentWeekStart.DayOfWeek + 7) % 7;
                            DateOnly nextDate = currentWeekStart.AddDays(daysToAdd);

                            if (nextDate > bookingDto.BookingDate && nextDate <= bookingDto.EndRepeatDate.Value)
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
                            }
                        }
                    }
                    currentWeekStart = currentWeekStart.AddDays(7); // Move to the next week
                }
            }
            return bookings;
        }

        /// <summary>
        /// adding Booking entry into the Dictionary
        /// </summary>
        /// <param name="groupedBookings"></param>
        /// <param name="booking"></param>
        private void AddBookingToDictionary(Dictionary<DateOnly, List<BookingCalendarDto>>groupedBookings, Booking booking, DateOnly date)
        {
            if(!groupedBookings.ContainsKey(date))
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

            if(!bookings.Any())
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
                foreach(var booking in item.Value)
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
