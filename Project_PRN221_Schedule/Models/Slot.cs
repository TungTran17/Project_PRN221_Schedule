using System;
using System.Collections.Generic;

namespace Project_PRN221_Schedule.Models
{
    public partial class Slot
    {
        public Slot()
        {
            WeekSchedules = new HashSet<WeekSchedule>();
        }

        public int Id { get; set; }
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;

        public virtual ICollection<WeekSchedule> WeekSchedules { get; set; }
    }
}
