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
        public WeekSchedule WeekSchedule { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            WeekSchedule = await _context.WeekSchedules
                .Include(ws => ws.Group).ThenInclude(g => g.Class)
                .Include(ws => ws.Group).ThenInclude(g => g.Course)
                .Include(ws => ws.Group).ThenInclude(g => g.Teacher)
                .Include(ws => ws.Room)
                .Include(ws => ws.Schedule)
                .Include(ws => ws.Slot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (WeekSchedule == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            WeekSchedule = await _context.WeekSchedules.FindAsync(id);

            if (WeekSchedule != null)
            {
                _context.WeekSchedules.Remove(WeekSchedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./ManagerSchedule");
        }
    }
}
