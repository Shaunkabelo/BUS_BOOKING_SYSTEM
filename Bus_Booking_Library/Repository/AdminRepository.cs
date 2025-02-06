using Bus_Booking_Library.DataAccess;
using Bus_Booking_Library.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus_Booking_Library.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ISqlDataAccess _db;

        public AdminRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<bool> InsertBus(int busId, string busNumber,string company, string busType, int totalSeats, int availableSeats)
        {
            var parameters = new
            {
                BusID = busId,
                BusNumber = busNumber,
                Company = company,
                BusType = busType,
                TotalSeats = totalSeats,
                AvailableSeats = availableSeats
            };

            await _db.SaveData("sp_InsertBus", parameters);
            return true;
        }

        public async Task<IEnumerable<Bus>> GetAllBuses()
        {
            return await _db.GetData<Bus, dynamic>("sp_GetAllBuses", new { });
        }

        public async Task<bool> InsertUser(int userId, string firstName, string lastName, string role, string email, string phone, string password)
        {
            var parameters = new
            {
                UserID = userId,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                Email = email,
                Phone = phone,
                Password = password
            };

            await _db.SaveData("sp_InsertUser", parameters);
            return true;
        }

        public async Task<bool> UpdateBus(int busId, string busNumber, string busType, int totalSeats, int availableSeats)
        {
            var parameters = new
            {
                BusID = busId,
                BusNumber = busNumber,
                BusType = busType,
                TotalSeats = totalSeats,
                AvailableSeats = availableSeats
            };

            await _db.SaveData("sp_UpdateBus", parameters);
            return true;
        }
    }

}
