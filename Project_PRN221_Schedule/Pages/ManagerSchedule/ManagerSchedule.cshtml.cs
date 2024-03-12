using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project_PRN221_Schedule.DAO;
using Project_PRN221_Schedule.Models;

namespace Project_PRN221_Schedule.Pages.ManagerSchedule
{
    public class ManagerScheduleModel : PageModel
    {
        private readonly Project_PRN221_ScheduleContext _context;

        public ManagerScheduleModel(Project_PRN221_ScheduleContext context)
        {
            _context = context;
        }

        public IList<WeekSchedule> WeekSchedule { get; set; }

        public async Task OnGetAsync()
        {
            WeekSchedule = await _context.WeekSchedules
                .Include(ws => ws.Room)
                .Include(ws => ws.Group).ThenInclude(g => g.Class)
                .Include(ws => ws.Group).ThenInclude(g => g.Course)
                .Include(ws => ws.Group).ThenInclude(g => g.Teacher)
                .Include(ws => ws.Slot)
                .ToListAsync();
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
                    if (await IsWeekScheduleExistingAsync(record.RoomCode, dayOfWeek1, dayOfWeek2))
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

                    // Nếu một trong số các khe thời gian không tồn tại trong cơ sở dữ liệu, trả về lỗi BadRequest
                    if (existingSlot1 == null || existingSlot2 == null)
                    {
                        return BadRequest("ID slot không tồn tại trong cơ sở dữ liệu.");
                    }

                    // Kiểm tra xem tuần-khe đã tồn tại trong cơ sở dữ liệu sau khi kiểm tra lại
                    if (await IsWeekScheduleExistingAsync(record.RoomCode, dayOfWeek1, dayOfWeek2))
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

                    // Tạo các bản ghi mới cho tuần-khe và thêm chúng vào cơ sở dữ liệu
                    var newWeekSchedule1 = new WeekSchedule
                    {
                        RoomId = roomId,
                        Group = new Group
                        {
                            ClassId = classId,
                            CourseId = courseId,
                            Teacher = new Teacher { TeacherName = record.TeacherName }
                        },
                        SlotId = slotId1,
                        WeekIndex = dayOfWeek1,
                        ScheduleId = scheduleId
                    };

                    _context.WeekSchedules.Add(newWeekSchedule1);

                    var newWeekSchedule2 = new WeekSchedule
                    {
                        RoomId = roomId,
                        Group = new Group
                        {
                            ClassId = classId,
                            CourseId = courseId,
                            Teacher = new Teacher { TeacherName = record.TeacherName }
                        },
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

        private async Task<bool> IsWeekScheduleExistingAsync(string roomCode, int dayOfWeek1, int dayOfWeek2)
        {
            return await _context.WeekSchedules
                .AnyAsync(ws => ws.Room.RoomCode == roomCode && (ws.WeekIndex == dayOfWeek1 || ws.WeekIndex == dayOfWeek2));
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
            // Truyền ngày dưới dạng chuỗi
            if (DateTime.TryParseExact(impDate, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                // Chuyển đổi ngày thành định dạng yyyy-MM-dd
                string formattedDate = parsedDate.ToString("yyyy-MM-dd");

                // Kiểm tra xem ngày đã chuyển đổi thành công hay không
                if (parsedDate != default(DateTime))
                {
                    var existingSchedule = await _context.Schedules.FirstOrDefaultAsync(s => s.ImplementDate == parsedDate);
                    if (existingSchedule != null)
                    {
                        return existingSchedule.Id;
                    }
                    else
                    {
                        // Nếu không tìm thấy, tạo bản ghi mới với ngày đã định dạng
                        return await CreateNewSchedule(formattedDate);
                    }
                }
                else
                {
                    // Ngày không được chuyển đổi thành công
                    throw new ArgumentException("Invalid date format.");
                }
            }
            else
            {
                // Xử lý định dạng ngày không hợp lệ
                // Ví dụ: ghi log lỗi hoặc ném một ngoại lệ
                throw new ArgumentException("Invalid date format.");
            }
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

        private async Task<int> CreateNewSchedule(String impDate)
        {
            // Parse the date string
            if (DateTime.TryParseExact(impDate, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                var newSchedule = new Schedule { ImplementDate = parsedDate };
                _context.Schedules.Add(newSchedule);
                await _context.SaveChangesAsync();
                return newSchedule.Id;
            }
            else
            {
                // Handle invalid date format
                // For example, log the error or throw an exception
                throw new ArgumentException("Invalid date format.");
            }
        }
    }
}
