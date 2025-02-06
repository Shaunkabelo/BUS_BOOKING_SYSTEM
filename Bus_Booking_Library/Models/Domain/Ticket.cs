using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Models.Domain
{
    public class Ticket
    {
        public int TicketID { get; set; }
        public int BookingID { get; set; }
        public string TicketNumber { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime TravelDate { get; set; }
        public string SeatNumber { get; set; }
        public string PassengerName { get; set; }

        // Navigation Property
        public Booking Booking { get; set; }
    }

}
