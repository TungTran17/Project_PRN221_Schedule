using System;
using System.Collections.Generic;

namespace Project_PRN221_Schedule.Models
{
    public partial class Group
    {
        public Group()
        {
            WeekSchedules = new HashSet<WeekSchedule>();
        }

        public int Id { get; set; }
        public int ClassId { get; set; }
        public int TeacherId { get; set; }
        public int CourseId { get; set; }

        public virtual Class Class { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
        public virtual Teacher Teacher { get; set; } = null!;
        public virtual ICollection<WeekSchedule> WeekSchedules { get; set; }
    }
}
