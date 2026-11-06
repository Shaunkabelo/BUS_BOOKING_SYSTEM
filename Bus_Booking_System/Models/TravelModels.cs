using System.ComponentModel.DataAnnotations;

namespace Bus_Booking_System.Models;

public class RegisterInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
public class LoginInput
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}
public class TravelUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";
}
public class Trip
{
    public int Id { get; set; }
    public int BusId { get; set; }
    public string Company { get; set; } = "";
    public string BusType { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public string BoardingPoint { get; set; } = "";
    public string ArrivalPoint { get; set; } = "";
    public DateTime Departure { get; set; }
    public DateTime Arrival { get; set; }
    public decimal Fare { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}
public class SearchInput : IValidatableObject
{
    [Required] public string Origin { get; set; } = "Johannesburg";
    [Required] public string Destination { get; set; } = "Durban";
    [DataType(DataType.Date)] public DateTime Date { get; set; } = TravelTime.Now.Date.AddDays(1);
    [Range(1, 6)] public int Passengers { get; set; } = 1;
    public List<Trip> Results { get; set; } = [];
    public List<string> Cities { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Origin == Destination) yield return new("Choose different departure and destination cities.");
        if (Date.Date < TravelTime.Now.Date || Date.Date > TravelTime.Now.Date.AddDays(90))
            yield return new("Choose a travel date within the next 90 days.", [nameof(Date)]);
    }
}
public class CheckoutInput
{
    public int TripId { get; set; }
    [Range(1, 6)] public int Passengers { get; set; } = 1;
    [Required] public int[] Seats { get; set; } = [];
    public List<PassengerInput> Travellers { get; set; } = [];
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the demo booking terms.")]
    public bool AcceptTerms { get; set; }
    public Guid RequestId { get; set; }
    public Trip? Trip { get; set; }
    public List<int> TakenSeats { get; set; } = [];
}
public class PassengerInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
}
public class Reservation
{
    public int Id { get; set; }
    public Guid Reference { get; set; }
    public int UserId { get; set; }
    public int TripId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateTime Departure { get; set; }
    public Trip? Trip { get; set; }
    public List<ReservedSeat> Seats { get; set; } = [];
}
public class ReservedSeat
{
    public int SeatNumber { get; set; }
    public string PassengerName { get; set; } = "";
}
public class FleetInput : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string BusNumber { get; set; } = "";
    [Required, StringLength(100)] public string Company { get; set; } = "";
    [Required, StringLength(50)] public string BusType { get; set; } = "Luxury";
    [Range(10, 80)] public int TotalSeats { get; set; } = 40;
    public IEnumerable<ValidationResult> Validate(ValidationContext context) => [];
}
public class ScheduleInput : IValidatableObject
{
    [Range(1, int.MaxValue)] public int BusId { get; set; }
    [Required, StringLength(100)] public string Origin { get; set; } = "";
    [Required, StringLength(100)] public string Destination { get; set; } = "";
    [Required, StringLength(200)] public string BoardingPoint { get; set; } = "";
    [Required, StringLength(200)] public string ArrivalPoint { get; set; } = "";
    public DateTime Departure { get; set; } = TravelTime.Now.Date.AddDays(1).AddHours(8);
    public DateTime Arrival { get; set; } = TravelTime.Now.Date.AddDays(1).AddHours(16);
    [Range(typeof(decimal), "1", "10000")] public decimal Fare { get; set; } = 350;
    public List<FleetInput> Buses { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Origin.Trim().Equals(Destination.Trim(), StringComparison.OrdinalIgnoreCase)) yield return new("Choose different cities.");
        if (Departure <= TravelTime.Now) yield return new("Departure must be in the future.");
        if (Arrival <= Departure) yield return new("Arrival must be after departure.");
    }
}
public static class TravelTime
{
    public static DateTime Now => DateTime.UtcNow.AddHours(2); // South Africa: UTC+2, no daylight saving.
    public static string Money(decimal amount) => amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-ZA"));
}
