using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPressureRecorder.Models
{
    public class PressureReading
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public int MinPressure { get; set; }
        public int MaxPressure { get; set; }
        public int HeartRate { get; set; }

        public DateTime MeasurementTime { get; set; }
    }
}
