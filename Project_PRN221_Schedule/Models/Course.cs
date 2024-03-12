using System;
using System.Collections.Generic;

namespace Project_PRN221_Schedule.Models
{
    public partial class Course
    {
        public Course()
        {
            Groups = new HashSet<Group>();
        }

        public int Id { get; set; }
        public string CourseCode { get; set; } = null!;

        public virtual ICollection<Group> Groups { get; set; }
    }
}
