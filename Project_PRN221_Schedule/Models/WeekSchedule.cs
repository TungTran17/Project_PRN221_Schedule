
namespace Project_PRN221_Schedule.Models
{
    public partial class WeekSchedule
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public int WeekIndex { get; set; }
        public int RoomId { get; set; }
        public int GroupId { get; set; }
        public int SlotId { get; set; }

        public virtual Group? Group { get; set; } = null!;
        public virtual Room? Room { get; set; } = null!;
        public virtual Schedule? Schedule { get; set; } = null!;
        public virtual Slot? Slot { get; set; } = null!;
    }
}
