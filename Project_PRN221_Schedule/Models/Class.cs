using System;
using System.Collections.Generic;

namespace Project_PRN221_Schedule.Models
{
    public partial class Class
    {
        public Class()
        {
            Groups = new HashSet<Group>();
        }

        public string? ClassName { get; set; }
        public int Id { get; set; }

        public virtual ICollection<Group> Groups { get; set; }
    }
}
