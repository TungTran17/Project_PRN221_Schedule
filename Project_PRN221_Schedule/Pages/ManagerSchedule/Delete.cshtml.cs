using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project_PRN221_Schedule.Models;

namespace Project_PRN221_Schedule.Pages.ManagerSchedule
{
    public class DeleteModel : PageModel
    {
        private readonly Project_PRN221_ScheduleContext _context;

        public DeleteModel(Project_PRN221_ScheduleContext context)
        {
            _context = context;
        }

        [BindProperty]
        public WeekSchedule WeekSchedule { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.WeekSchedules == null)
            {
                return NotFound();
            }

            var weekschedule = await _context.WeekSchedules.FirstOrDefaultAsync(m => m.Id == id);

            if (weekschedule == null)
            {
                return NotFound();
            }
            else
            {
                WeekSchedule = weekschedule;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null || _context.WeekSchedules == null)
            {
                return NotFound();
            }
            var weekschedule = await _context.WeekSchedules.FindAsync(id);

            if (weekschedule != null)
            {
                WeekSchedule = weekschedule;
                _context.WeekSchedules.Remove(WeekSchedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./ManagerSchedule");
        }
    }
}
