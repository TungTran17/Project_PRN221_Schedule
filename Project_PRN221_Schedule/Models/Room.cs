using System;
using System.Collections.Generic;

namespace Project_PRN221_Schedule.Models
{
    public partial class Room
    {
        public Room()
        {
            WeekSchedules = new HashSet<WeekSchedule>();
        }

        public string RoomCode { get; set; } = null!;
        public int Id { get; set; }

        public virtual ICollection<WeekSchedule> WeekSchedules { get; set; }
    }
}
