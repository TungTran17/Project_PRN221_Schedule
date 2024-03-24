using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NuGet.DependencyResolver;
using Project_PRN221_Schedule.DAO;
using Project_PRN221_Schedule.Models;

namespace Project_PRN221_Schedule.Pages.ManagerSchedule
{
    public class ManagerScheduleModel : PageModel
    {
        private readonly Project_PRN221_ScheduleContext _context;
        public DateTime SelectedDate { get; set; }

        public ManagerScheduleModel(Project_PRN221_ScheduleContext context)
        {
            _context = context;
            SelectedDate = DateTime.Today;
            Slots = _context.Slots.ToList();
        }

        public IList<WeekSchedule> WeekSchedule { get; set; }
        public List<Slot> Slots { get; set; }

        public async Task OnGetAsync()
        {
            WeekSchedule = FilterSchedule(SelectedDate);
        }

        public async Task<IActionResult> OnPostFilter(DateTime? selectedDate)
        {
            SelectedDate = selectedDate ?? DateTime.Today;
            WeekSchedule = FilterSchedule(selectedDate ?? DateTime.Today);

            return Page();
        }

        public List<WeekSchedule> FilterSchedule(DateTime selectedDate)
        {
            List<WeekSchedule> rs = new List<WeekSchedule>();
            var monday = GetMonday(selectedDate);

            Schedule? schedule = _context.Schedules.FirstOrDefault(s => monday <= s.ImplementDate && s.ImplementDate <= monday.AddDays(6));

            if (schedule != null)
            {
                rs = _context.WeekSchedules
                    .Include(ws => ws.Room)
                    .Include(ws => ws.Group).ThenInclude(g => g.Class)
                    .Include(ws => ws.Group).ThenInclude(g => g.Course)
                    .Include(ws => ws.Group).ThenInclude(g => g.Teacher)
                    .Include(ws => ws.Slot)
                    .Where(ws => ws.ScheduleId == schedule.Id && ws.WeekIndex == ConvertDayOfWeekToWeekIndex(selectedDate.DayOfWeek))
                    .ToList();
            }
            return rs;
        }


        private int ConvertDayOfWeekToWeekIndex(DayOfWeek dayOfWeek)
        {
            if (dayOfWeek == DayOfWeek.Sunday)
            {
                return 8;
            }
            else
            {
                return (int)dayOfWeek + 1;
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

        public async Task<IActionResult> OnPostImportCSVAsync(IFormFile csvFile)
        {
            // Kiểm tra xem tệp CSV có được chọn hay không
            if (csvFile == null || csvFile.Length == 0)
            {
                // Nếu không có tệp CSV được chọn, chuyển hướng người dùng trở lại trang hiện tại
                return RedirectToPage();
            }

            // Khởi tạo một HashSet để theo dõi các bộ kết hợp tuần-khe đã được import từ tệp CSV
            var importedWeekSlotCombos = new HashSet<string>();

            // Khởi tạo một Dictionary để lưu trữ thông tin về các tuần-khe đã tồn tại
            var existingWeekSchedules = new Dictionary<string, HashSet<int>>();

            // Đọc dữ liệu từ tệp CSV và xử lý từng bản ghi
            using (var reader = new StreamReader(csvFile.OpenReadStream()))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null, // Vô hiệu hóa kiểm tra tiêu đề CSV
                MissingFieldFound = null // Vô hiệu hóa thông báo khi trường bị thiếu trong CSV
            }))
            {
                var records = csv.GetRecords<CsvRecord>();
                foreach (var record in records)
                {
                    // Xử lý từng bản ghi từ tệp CSV

                    // Lấy thông tin về khe thời gian từ trường TimeSlot của bản ghi
                    string timeSlot = record.TimeSlot;

                    // Kiểm tra xem trường TimeSlot có hợp lệ không
                    if (string.IsNullOrEmpty(timeSlot) || timeSlot.Length < 3)
                    {
                        // Nếu không hợp lệ, bỏ qua bản ghi và tiếp tục với bản ghi tiếp theo
                        continue;
                    }

                    // Lấy loại khe thời gian (A hoặc P) và chỉ mục của khe thời gian
                    char slotType = timeSlot[0];
                    int slotIndex1 = int.Parse(timeSlot[1].ToString());
                    int slotIndex2 = int.Parse(timeSlot[2].ToString());

                    int slotId1, slotId2;

                    // Xác định ID của khe thời gian dựa trên loại khe thời gian
                    switch (slotType)
                    {
                        case 'A':
                            slotId1 = 1;
                            slotId2 = 2;
                            break;
                        case 'P':
                            slotId1 = 3;
                            slotId2 = 4;
                            break;
                        default:
                            throw new ArgumentException("Loại slot không hợp lệ."); // Ném ngoại lệ nếu loại khe thời gian không hợp lệ
                    }

                    // Xác định ngày trong tuần cho mỗi khe thời gian
                    int dayOfWeek1 = slotIndex1;
                    int dayOfWeek2 = slotIndex2;

                    // Tạo chuỗi kết hợp của tuần và khe thời gian
                    string weekSlotCombo1 = $"{dayOfWeek1}_{slotId1}";
                    string weekSlotCombo2 = $"{dayOfWeek2}_{slotId2}";

                    // Kiểm tra xem bộ kết hợp tuần-khe đã được import chưa, nếu có thì bỏ qua
                    if (importedWeekSlotCombos.Contains(weekSlotCombo1) || importedWeekSlotCombos.Contains(weekSlotCombo2))
                    {
                        continue;
                    }

                    // Thêm bộ kết hợp tuần-khe đã import vào HashSet để tránh trùng lặp
                    importedWeekSlotCombos.Add(weekSlotCombo1);
                    importedWeekSlotCombos.Add(weekSlotCombo2);

                    // Kiểm tra xem tuần-khe đã tồn tại trong cơ sở dữ liệu hay không, nếu có thì bỏ qua
                    if (await IsWeekScheduleExistingAsync(record.ScheduleId, record.RoomCode, dayOfWeek1, dayOfWeek2, record.impDate, record.TimeSlot, record.ClassName, record.RoomCode, record.TeacherName, record.CourseCode))
                    {
                        continue;
                    }

                    // Lấy hoặc tạo ID cho phòng, lớp, khóa học và lịch trình từ dữ liệu CSV
                    int roomId = await GetOrCreateRoomIdAsync(record.RoomCode);
                    int classId = await GetOrCreateClassIdAsync(record.ClassName);
                    int courseId = await GetOrCreateCourseIdAsync(record.CourseCode);
                    int scheduleId = await GetOrCreateScheduleIdAsync(record.impDate);

                    // Kiểm tra xem các khe thời gian đã tồn tại trong cơ sở dữ liệu hay không
                    var existingSlot1 = await _context.Slots.FirstOrDefaultAsync(s => s.Id == slotId1);
                    var existingSlot2 = await _context.Slots.FirstOrDefaultAsync(s => s.Id == slotId2);
                    var existingTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherName == record.TeacherName);
                    var existingGroup = await _context.Groups.FirstOrDefaultAsync(g =>
                                        g.Class.ClassName == record.ClassName &&
                                        g.Course.CourseCode == record.CourseCode &&
                                        g.Teacher.TeacherName == record.TeacherName);

                    // Nếu một trong số các khe thời gian không tồn tại trong cơ sở dữ liệu, trả về lỗi BadRequest
                    if (existingSlot1 == null || existingSlot2 == null)
                    {
                        return BadRequest("ID slot không tồn tại trong cơ sở dữ liệu.");
                    }

                    // Kiểm tra xem tuần-khe đã tồn tại trong cơ sở dữ liệu sau khi kiểm tra lại
                    if (await IsWeekScheduleExistingAsync(record.ScheduleId, record.RoomCode, dayOfWeek1, dayOfWeek2, record.impDate, record.TimeSlot, record.ClassName, record.RoomCode, record.TeacherName, record.CourseCode))
                    {
                        continue;
                    }


                    // Nếu tuần-khe chưa tồn tại trong cơ sở dữ liệu, thêm nó vào Dictionary để theo dõi
                    if (!existingWeekSchedules.ContainsKey(record.RoomCode))
                    {
                        existingWeekSchedules[record.RoomCode] = new HashSet<int>();
                    }
                    existingWeekSchedules[record.RoomCode].Add(dayOfWeek1);
                    existingWeekSchedules[record.RoomCode].Add(dayOfWeek2);

                    // Tạo mới Group nếu không tồn tại
                    Group group;
                    if (existingGroup != null)
                    {
                        // Sử dụng Group đã tồn tại
                        group = existingGroup;
                    }
                    else
                    {
                        // Tạo mới Group
                        group = new Group
                        {
                            ClassId = classId,
                            CourseId = courseId,
                            Teacher = existingTeacher // Sử dụng giáo viên đã tạo hoặc đã có
                        };
                        _context.Groups.Add(group);
                        await _context.SaveChangesAsync(); // Lưu Group mới vào cơ sở dữ liệu
                    }

                    // Sử dụng Group đã được tạo hoặc sử dụng từ cơ sở dữ liệu
                    var newWeekSchedule1 = new WeekSchedule
                    {
                        RoomId = roomId,
                        Group = group, // Sử dụng Group đã tạo hoặc đã có
                        SlotId = slotId1,
                        WeekIndex = dayOfWeek1,
                        ScheduleId = scheduleId
                    };

                    _context.WeekSchedules.Add(newWeekSchedule1);

                    var newWeekSchedule2 = new WeekSchedule
                    {
                        RoomId = roomId,
                        Group = group, // Sử dụng Group đã tạo hoặc đã có
                        SlotId = slotId2,
                        WeekIndex = dayOfWeek2,
                        ScheduleId = scheduleId
                    };

                    _context.WeekSchedules.Add(newWeekSchedule2);
                }

                // Lưu các thay đổi vào cơ sở dữ liệu sau khi hoàn thành xử lý tệp CSV
                await _context.SaveChangesAsync();
            }

            // Chuyển hướng người dùng trở lại trang hiện tại sau khi import dữ liệu từ tệp CSV thành công
            return RedirectToPage();
        }

        private async Task<bool> IsWeekScheduleExistingAsync(int scheduleId, string roomCode, int dayOfWeek1, int dayOfWeek2, string impDate, string timeSlot, string className, string roomCodeCheck, string teacherName, string subjectName)
        {
            // Chuyển đổi ImpDate từ chuỗi sang kiểu DateTime
            DateTime impDateTime = DateTime.ParseExact(impDate, "M/d/yyyy", CultureInfo.InvariantCulture);

            return await _context.WeekSchedules
                .AnyAsync(ws =>
                    (
                            (ws.ScheduleId == scheduleId && (ws.WeekIndex == dayOfWeek1 || ws.WeekIndex == dayOfWeek2) && ws.Schedule.ImplementDate == impDateTime) && // Điều kiện chính
                            (
                                // Kiểm tra các điều kiện phụ nếu scheduleId và impDateTime giống nhau
                                (ws.Room.RoomCode == roomCode && ws.Group.Class.ClassName == className && ws.Group.Teacher.TeacherName != teacherName && ws.Group.Course.CourseCode != subjectName) ||
                                (ws.Room.RoomCode == roomCode && ws.Group.Class.ClassName == className && ws.Group.Teacher.TeacherName == teacherName && ws.Group.Course.CourseCode == subjectName) ||
                                (ws.Room.RoomCode == roomCode && ws.Group.Teacher.TeacherName == teacherName && ws.Group.Class.ClassName == className && ws.Group.Course.CourseCode != subjectName) ||
                                (ws.Room.RoomCode != roomCode && ws.Group.Class.ClassName == className && ws.Group.Teacher.TeacherName == teacherName) ||
                                (ws.Room.RoomCode == roomCode && ws.Group.Class.ClassName == className && ws.Group.Teacher.TeacherName != teacherName)
                            )

                    )
                );
        }

        private async Task<int> GetOrCreateRoomIdAsync(string roomCode)
        {
            var existingRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
            if (existingRoom != null)
            {
                return existingRoom.Id;
            }
            else
            {
                return await CreateNewRoom(roomCode);
            }
        }

        private async Task<int> GetOrCreateClassIdAsync(string className)
        {
            var existingClass = await _context.Classes.FirstOrDefaultAsync(c => c.ClassName == className);
            if (existingClass != null)
            {
                return existingClass.Id;
            }
            else
            {
                return await CreateNewClass(className);
            }
        }

        private async Task<int> GetOrCreateCourseIdAsync(string courseCode)
        {
            var existingCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseCode == courseCode);
            if (existingCourse != null)
            {
                return existingCourse.Id;
            }
            else
            {
                return await CreateNewCourse(courseCode);
            }
        }

        private async Task<int> GetOrCreateScheduleIdAsync(string impDate)
        {
            // Chuyển đổi ImpDate từ chuỗi sang kiểu DateTime
            DateTime impDateTime;
            if (!DateTime.TryParseExact(impDate, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out impDateTime))
            {
                throw new ArgumentException("Invalid date format.");
            }

            // Kiểm tra xem ngày đã tồn tại trong cơ sở dữ liệu hay chưa, nếu có thì trả về ID
            var existingSchedule = await _context.Schedules.FirstOrDefaultAsync(s => s.ImplementDate == impDateTime);
            if (existingSchedule != null)
            {
                return existingSchedule.Id;
            }

            // Nếu không tồn tại, tạo mới một ngày và lưu vào cơ sở dữ liệu
            var newSchedule = new Schedule { ImplementDate = impDateTime };
            _context.Schedules.Add(newSchedule);
            await _context.SaveChangesAsync();

            return newSchedule.Id;
        }


        private async Task<int> CreateNewRoom(string roomCode)
        {
            var newRoom = new Room { RoomCode = roomCode };
            _context.Rooms.Add(newRoom);
            await _context.SaveChangesAsync();
            return newRoom.Id;
        }

        private async Task<int> CreateNewCourse(string courseCode)
        {
            var newCourse = new Course { CourseCode = courseCode };
            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();
            return newCourse.Id;
        }

        // Tạo lớp mới và chèn vào cơ sở dữ liệu
        private async Task<int> CreateNewClass(string className)
        {
            var newClass = new Class { ClassName = className };
            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();
            return newClass.Id;
        }
    }
}
