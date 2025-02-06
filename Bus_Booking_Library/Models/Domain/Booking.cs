using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Models.Domain
{
    public class Booking
    {
        public int BookingID { get; set; }
        public int UserID { get; set; }
        public int BusID { get; set; }
        public int RouteID { get; set; }
        public int NumberOfSeats { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public Bus Bus { get; set; }
        public Route Route { get; set; }
    }

}
