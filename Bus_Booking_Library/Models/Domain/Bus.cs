using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Models.Domain
{
    public class Bus
    {
        public int BusID { get; set; }
        public string BusNumber { get; set; }

        public string Company {  get; set; }
        public string BusType { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        
    }

}
