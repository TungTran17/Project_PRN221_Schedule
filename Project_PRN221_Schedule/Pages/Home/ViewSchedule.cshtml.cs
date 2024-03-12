using System;
using System.Collections.Generic;
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

        public ViewScheduleModel(Project_PRN221_ScheduleContext context)
        {
            _context = context;
        }

        public string StartTimeFormatted { get; set; }
        public string EndTimeFormatted { get; set; }

        public IList<Slot> Slots { get; set; }
        public IList<WeekSchedule> WeekSchedule { get; set; }

        public async Task OnGetAsync(int? week, int? year)
        {
            await LoadSlotsAsync();
            await LoadWeekScheduleAsync();

            if (!week.HasValue || !year.HasValue)
            {
                // Mặc định hiển thị dữ liệu cho tuần và năm hiện tại
                week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                year = DateTime.Today.Year;
            }
        }

        private async Task LoadSlotsAsync()
        {
            Slots = await _context.Slots.ToListAsync();
        }

        private async Task LoadWeekScheduleAsync()
        {
            if (_context.WeekSchedules != null)
            {
                WeekSchedule = await _context.WeekSchedules
                    .Include(w => w.Group).ThenInclude(g => g.Class)
                    .Include(w => w.Group).ThenInclude(g => g.Course)
                    .Include(w => w.Group).ThenInclude(g => g.Teacher)
                    .Include(w => w.Room)
                    .Include(w => w.Slot)
                    .ToListAsync();
            }
        }
    }
}
