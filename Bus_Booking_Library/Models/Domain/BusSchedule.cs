using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Models.Domain
{
    public class BusSchedule
    {
        public int ScheduleID { get; set; }
        public int BusID { get; set; }
        public int RouteID { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        // Navigation Properties
        public Bus Bus { get; set; }
        public Route Route { get; set; }
    }

}
