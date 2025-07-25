using MyPressureRecorder.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPressureRecorder.Data
{
    public class AppDatabase
    {
        private readonly SQLiteAsyncConnection _database;

        public AppDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<UserProfile>().Wait();
            _database.CreateTableAsync<PressureReading>().Wait();

        }

        public Task<List<UserProfile>> GetUsersAsync() => _database.Table<UserProfile>().ToListAsync();
        public Task<int> SaveUserAsync(UserProfile user) => _database.InsertAsync(user);
        public Task<int> DeleteUserAsync(UserProfile user) => _database.DeleteAsync(user);


        #region PRESSURE READINGS
        public Task<List<PressureReading>> GetReadingsByUserAsync(Guid userId) => _database.Table<PressureReading>()
                                                                                         .Where(r => r.UserId == userId)
                                                                                         .OrderByDescending(r => r.MeasurementTime)
                                                                                         .ToListAsync();

        public Task<int> SaveReadingAsync(PressureReading reading) => _database.InsertAsync(reading);

        #endregion
    }
}
