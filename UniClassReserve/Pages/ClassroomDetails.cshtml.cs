using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UniClassReserve.Data;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace UniClassReserve.Pages
{
    [Authorize(Roles = "Admin")]
    public class ClassroomDetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<UniClassReserve.Data.ApplicationUser> _userManager;
        public ClassroomDetailsModel(AppDbContext context, UserManager<UniClassReserve.Data.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public Classroom? Classroom { get; set; }
        public List<Feedback> Feedbacks { get; set; } = new();
        public List<Reservation> Reservations { get; set; } = new();
        public List<DaySchedule> WeeklySchedule { get; set; } = new();
        public class DaySchedule
        {
            public string DayName { get; set; } = "";
            public List<SlotInfo> Slots { get; set; } = new();
        }
        public class SlotInfo
        {
            public string TimeRange { get; set; } = ""; // 11:00-12:00 (First Term)
            public string Type { get; set; } = "Normal"; // Normal, Holiday, Conflict
            public string Tooltip { get; set; } = "";
        }
        public async Task<IActionResult> OnGetAsync(int id, string? sort)
        {
            Classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
            if (Classroom == null) return Page();
            var feedbacksQuery = _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Term)
                .Where(f => f.ClassroomId == id && f.User != null && !f.User.IsDeleted);
            switch (sort)
            {
                case "date_asc":
                    feedbacksQuery = feedbacksQuery.OrderBy(f => f.CreatedAt);
                    break;
                case "rating_desc":
                    feedbacksQuery = feedbacksQuery.OrderByDescending(f => f.Rating);
                    break;
                case "rating_asc":
                    feedbacksQuery = feedbacksQuery.OrderBy(f => f.Rating);
                    break;
                default:
                    feedbacksQuery = feedbacksQuery.OrderByDescending(f => f.CreatedAt);
                    break;
            }
            Feedbacks = await feedbacksQuery.ToListAsync();
            Reservations = await _context.Reservations
                .Include(r => r.Term)
                .Include(r => r.User)
                .Where(r => r.ClassroomId == id && r.Status == "Approved" && r.User != null && !r.User.IsDeleted)
                .ToListAsync();
            WeeklySchedule = new List<DaySchedule>();
            for (int i = 1; i <= 7; i++)
            {
                var dayName = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName((DayOfWeek)(i % 7));
                var slots = new List<SlotInfo>();
                var dayReservations = Reservations
                    .Where(r => r.DayOfWeek == i)
                    .OrderBy(r => r.StartTime)
                    .ToList();
                foreach (var r in dayReservations)
                {
                    string type = "Normal";
                    string tooltip = $"{r.StartTime:HH:mm}-{r.EndTime:HH:mm} ({r.Term?.Name ?? "-"})";
                    // Tatil kontrolü (gün bazında)
                    bool isHoliday = false;
                    try
                    {
                        var holidayService = HttpContext.RequestServices.GetService(typeof(UniClassReserve.Data.IHolidayService)) as UniClassReserve.Data.IHolidayService;
                        if (holidayService != null)
                        {
                            isHoliday = holidayService.IsHolidayAsync(r.StartTime).GetAwaiter().GetResult();
                        }
                    }
                    catch { }
                    // Çakışma kontrolü (aynı gün/saatte başka rezervasyon var mı?)
                    bool isConflict = Reservations.Any(x => x.Id != r.Id && x.DayOfWeek == r.DayOfWeek && x.Status == "Approved" && ((x.StartTime < r.EndTime && x.EndTime > r.StartTime)));
                    if (isHoliday)
                    {
                        type = "Holiday";
                        tooltip += " | Public Holiday";
                    }
                    else if (isConflict)
                    {
                        type = "Conflict";
                        tooltip += " | Conflict";
                    }
                    slots.Add(new SlotInfo { TimeRange = tooltip, Type = type, Tooltip = tooltip });
                }
                WeeklySchedule.Add(new DaySchedule { DayName = dayName, Slots = slots });
            }
            return Page();
        }

        [BindProperty]
        public int Rating { get; set; }
        [BindProperty]
        public string Comment { get; set; } = string.Empty;
        [BindProperty]
        public int ClassroomId { get; set; }
        public async Task<IActionResult> OnPostLeaveFeedbackAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Instructor"))
            {
                ModelState.AddModelError(string.Empty, "Only instructors can leave feedback.");
                return await OnGetAsync(ClassroomId, null);
            }
            // Aynı kullanıcı aynı sınıfa birden fazla feedback bırakamasın
            bool alreadyLeft = await _context.Feedbacks.AnyAsync(f => f.UserId == userId && f.ClassroomId == ClassroomId);
            if (alreadyLeft)
            {
                ModelState.AddModelError(string.Empty, "You have already left feedback for this classroom.");
                return await OnGetAsync(ClassroomId, null);
            }
            var feedback = new Feedback
            {
                UserId = userId!,
                ClassroomId = ClassroomId,
                Rating = Rating,
                Comment = Comment,
                CreatedAt = DateTime.Now
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = ClassroomId });
        }
    }
} 