using Bus_Booking_Library.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Repository
{
    public interface IAdminRepository
    {
        //BUS
        Task<bool> InsertBus(int BusId, string BusNumber,string Company, string BusType, int TotalSeats, int AvailableSeats);
        Task<IEnumerable<Bus>> GetAllBuses();
        Task<bool> UpdateBus(int busId, string busNumber, string busType, int totalSeats, int availableSeats);

        //USER

        Task<bool> InsertUser (int UserID, string FirstName, string LastName, string Role, string Email, string phone, string Password);


    }
}
