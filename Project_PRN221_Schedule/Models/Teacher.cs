using System;
using System.Collections.Generic;

namespace Project_PRN221_Schedule.Models
{
    public partial class Teacher
    {
        public Teacher()
        {
            Groups = new HashSet<Group>();
        }

        public int Id { get; set; }
        public string TeacherName { get; set; } = null!;

        public virtual ICollection<Group> Groups { get; set; }
    }
}
