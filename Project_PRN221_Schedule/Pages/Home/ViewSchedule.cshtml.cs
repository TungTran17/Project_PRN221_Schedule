using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project_PRN221_Schedule.Models;

namespace Project_PRN221_Schedule.Pages.Home
{
    public class ViewScheduleModel : PageModel
    {
        private readonly Project_PRN221_ScheduleContext _context;
        //public DateTime LaunchDate { get; set; }
        private readonly int TOTAL_DAY_OF_WEEK = 7;

        public ViewScheduleModel(Project_PRN221_ScheduleContext context)
        {
            _context = context;
            SelectedDate = GetMonday(DateTime.Today);
            Slots = _context.Slots.ToList();
            //LaunchDate = _context.Schedules.Select(s => s.ImplementDate).GroupBy(g=> g.Date).Select(g => g.Min()).FirstOrDefault();
        }
        public DateTime SelectedDate { get; set; }

        public IList<Slot> Slots { get; set; }
        public IList<WeekSchedule> WeekSchedule { get; set; }

        public async Task OnGetAsync(int? week, int? year)
        {
            // Ensure WeekSchedule is initialized
            if (WeekSchedule == null)
            {
                WeekSchedule = new List<WeekSchedule>();
            }

            // Load WeekSchedule data if it's not already loaded
            if (week.HasValue || year.HasValue)
            {
                SelectedDate = GetMonday(week, year);
            }
            await LoadWeekScheduleAsync(SelectedDate);
        }

        public async Task<IActionResult> OnPost(int? week, int? year)
        {

            if (WeekSchedule == null)
            {
                WeekSchedule = new List<WeekSchedule>();
            }

            if (week.HasValue || year.HasValue)
            {
                SelectedDate = GetMonday(week, year);
                // Load WeekSchedule based on the selected week and year
                await LoadWeekScheduleAsync(SelectedDate);
            }
            // Redirect back to the GET handler to display the filtered data
            return Page();
        }

        private DateTime GetMonday(int? week, int? year)
        {
            return GetMonday(new DateTime(year ?? DateTime.Now.Year, 1, 4)).AddDays(((week ?? 0) - 1) * TOTAL_DAY_OF_WEEK);
        }

        private async Task LoadWeekScheduleAsync(DateTime SelectedDate)
        {
            DateTime monday = GetMonday(SelectedDate);
            Schedule? x = _context.Schedules.Where(s => s.ImplementDate >= monday && s.ImplementDate <= monday.AddDays(6)).FirstOrDefault();
            if (x != null)
            {
                WeekSchedule = await _context.WeekSchedules
                    .Include(w => w.Group).ThenInclude(g => g.Class)
                    .Include(w => w.Group).ThenInclude(g => g.Course)
                    .Include(w => w.Group).ThenInclude(g => g.Teacher)
                    .Include(w => w.Room)
                    .Include(w => w.Slot).Where(w => w.ScheduleId == x.Id)
                    .ToListAsync();
            }
        }

        private DateTime GetMonday(DateTime selectedDate)
        {
            if (selectedDate.DayOfWeek == DayOfWeek.Sunday)
            {
                return selectedDate.AddDays(-6);
            }
            return selectedDate.AddDays(-(int)selectedDate.DayOfWeek + 1);
        }

    }
}
