using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_PRN221_Schedule.Models;

namespace Project_PRN221_Schedule.Pages.ManagerSchedule
{
    public class EditScheduleModel : PageModel
    {
        private readonly Project_PRN221_ScheduleContext _context;

        public EditScheduleModel(Project_PRN221_ScheduleContext context)
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
            WeekSchedule = weekschedule;
            
            // Retrieve necessary data for dropdowns
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "RoomCode");
            ViewData["SlotId"] = new SelectList(_context.Slots, "Id", "Id"); // Assuming SlotId is mapped to StartTime

            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // If ModelState is not valid, return the page with validation errors
                return Page();
            }

            _context.Attach(WeekSchedule).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WeekScheduleExists(WeekSchedule.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Home/ViewSchedule");
        }

        private bool WeekScheduleExists(int id)
        {
            return (_context.WeekSchedules?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
