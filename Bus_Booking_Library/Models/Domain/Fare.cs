using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Models.Domain
{
    public class Fare
    {
        public int FareID { get; set; }
        public int RouteID { get; set; }
        public decimal FareAmount { get; set; }

        // Navigation Property
        public Route Route { get; set; }
    }

}
