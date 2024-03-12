using System;
using System.Collections.Generic;

namespace Project_PRN221_Schedule.Models
{
    public partial class Schedule
    {
        public Schedule()
        {
            WeekSchedules = new HashSet<WeekSchedule>();
        }

        public int Id { get; set; }
        public DateTime ImplementDate { get; set; }

        public virtual ICollection<WeekSchedule> WeekSchedules { get; set; }
    }
}
