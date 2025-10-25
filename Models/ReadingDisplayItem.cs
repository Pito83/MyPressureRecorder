using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPressureRecorder.Models
{
    public class ReadingDisplayItem
    {
        public PressureReading Reading { get; set; }
        public string DisplayString => $"{Reading.MaxPressure}/{Reading.MinPressure} mmHg - {Reading.HeartRate} bpm";
        public DateTime MeasurementTime => Reading.MeasurementTime;
        public bool IsSelected { get; set; }
    }

}
