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
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Id");
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id");
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "Id", "Id");
            ViewData["SlotId"] = new SelectList(_context.Slots, "Id", "Id");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
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

            return RedirectToPage("./Index");
        }

        private bool WeekScheduleExists(int id)
        {
            return (_context.WeekSchedules?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
