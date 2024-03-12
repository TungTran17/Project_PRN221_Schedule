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
            if (csvFile == null || csvFile.Length == 0)
            {
                return RedirectToPage(); // Redirect back to the page if no file is uploaded
            }

            using (var reader = new StreamReader(csvFile.OpenReadStream()))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null, // Turn off header validation
                MissingFieldFound = null // Ignore missing fields
            }))
            {
                var records = csv.GetRecords<CsvRecord>();
                foreach (var record in records)
                {
                    string timeSlot = record.TimeSlot;

                    if (string.IsNullOrEmpty(timeSlot) || timeSlot.Length < 3)
                    {
                        // Handle invalid TimeSlot string
                        // For example, log the error or skip this record
                        continue;
                    }

                    char slotType = timeSlot[0];
                    int slotIndex1 = int.Parse(timeSlot[1].ToString());
                    int slotIndex2 = int.Parse(timeSlot[2].ToString());

                    // Determine SlotIds corresponding to the slot type and indices
                    int slotId1, slotId2;

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
                            throw new ArgumentException("Invalid slot type.");
                    }

                    int dayOfWeek1 = slotIndex1;
                    int dayOfWeek2 = slotIndex2;

                    // Check if RoomCode exists in the database
                    int roomId = await GetOrCreateRoomIdAsync(record.RoomCode);

                    // Kiểm tra xem ClassName đã tồn tại trong cơ sở dữ liệu chưa
                    int classId = await GetOrCreateClassIdAsync(record.ClassName);

                    // Check if CourseCode exists in the database
                    int courseId = await GetOrCreateCourseIdAsync(record.CourseCode);

                    // Check if Schedule exists in the database
                    int scheduleId = await GetOrCreateScheduleIdAsync(record.impDate);

                    // Check if the SlotIds exist in the database
                    var existingSlot1 = await _context.Slots.FirstOrDefaultAsync(s => s.Id == slotId1);
                    var existingSlot2 = await _context.Slots.FirstOrDefaultAsync(s => s.Id == slotId2);

                    if (existingSlot1 == null || existingSlot2 == null)
                    {
                        // If the slot doesn't exist, return an error or handle it appropriately
                        return BadRequest("Slot ID does not exist in the database.");
                    }

                    // Check if a schedule with the same properties exists but with different WeekIndex
                    var existingWeekSchedule1 = await _context.WeekSchedules.FirstOrDefaultAsync(ws =>
                        ws.Room.RoomCode == record.RoomCode &&
                        ws.WeekIndex != dayOfWeek1 &&
                        ws.SlotId == slotId1 &&
                        ws.ScheduleId == scheduleId);

                    if (existingWeekSchedule1 != null)
                    {
                        // Update WeekIndex if week schedule exists with different WeekIndex
                        existingWeekSchedule1.WeekIndex = dayOfWeek1;
                        _context.WeekSchedules.Update(existingWeekSchedule1);
                    }
                    else
                    {
                        // Create a new WeekSchedule record
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

                        // Add new WeekSchedule to the database
                        _context.WeekSchedules.Add(newWeekSchedule1);
                    }

                    var existingWeekSchedule2 = await _context.WeekSchedules.FirstOrDefaultAsync(ws =>
                        ws.Room.RoomCode == record.RoomCode &&
                        ws.WeekIndex != dayOfWeek2 &&
                        ws.SlotId == slotId2 &&
                        ws.ScheduleId == scheduleId);

                    if (existingWeekSchedule2 != null)
                    {
                        // Update WeekIndex if week schedule exists with different WeekIndex
                        existingWeekSchedule2.WeekIndex = dayOfWeek2;
                        _context.WeekSchedules.Update(existingWeekSchedule2);
                    }
                    else
                    {
                        // Create a new WeekSchedule record
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

                        // Add new WeekSchedule to the database
                        _context.WeekSchedules.Add(newWeekSchedule2);
                    }
                }

                // Save changes to the database
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(); // Redirect back to the page after import
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
