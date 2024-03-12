using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_PRN221_Schedule.Models;

namespace Project_PRN221_Schedule.Pages.ManagerSchedule
{
    public class EditModel : PageModel
    {
        private readonly Project_PRN221_ScheduleContext _context;

        public EditModel(Project_PRN221_ScheduleContext context)
        {
            _context = context;
        }

        [BindProperty]
        public WeekSchedule WeekSchedule { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            WeekSchedule = await _context.WeekSchedules
                .Include(ws => ws.Room)
                .Include(ws => ws.Slot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (WeekSchedule == null)
            {
                return NotFound();
            }

            // Tạo danh sách các RoomCode
            ViewData["RoomCode"] = _context.Rooms.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(), // Sử dụng Id thay vì RoomCode
                Text = r.RoomCode
            });

            // Tạo danh sách các SlotId
            ViewData["SlotId"] = _context.Slots.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.StartTime} - {s.EndTime}"
            });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Any())
                    {
                        foreach (var error in state.Errors)
                        {
                            // Log or print error message for debugging
                            Console.WriteLine($"Error in {key}: {error.ErrorMessage}");
                        }
                    }
                }
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

            return RedirectToPage("./ManagerSchedule");
        }

        private bool WeekScheduleExists(int id)
        {
            return _context.WeekSchedules.Any(e => e.Id == id);
        }
    }
}
