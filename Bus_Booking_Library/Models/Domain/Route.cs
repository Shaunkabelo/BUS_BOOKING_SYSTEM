using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Models.Domain
{
    public class Route
    {
        public int RouteID { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public string Distance { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }

}
