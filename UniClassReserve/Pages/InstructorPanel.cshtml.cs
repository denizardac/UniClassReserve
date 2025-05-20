using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UniClassReserve.Data;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

namespace UniClassReserve.Pages
{
    [Authorize(Roles = "Instructor")]
    public class InstructorPanelModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHolidayService _holidayService;
        private readonly ILogService _logService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly UserManager<UniClassReserve.Data.ApplicationUser> _userManager;

        public InstructorPanelModel(AppDbContext context, IHolidayService holidayService, ILogService logService, IEmailService emailService, IConfiguration config, UserManager<UniClassReserve.Data.ApplicationUser> userManager)
        {
            _context = context;
            _holidayService = holidayService;
            _logService = logService;
            _emailService = emailService;
            _config = config;
            _userManager = userManager;
        }

        public List<Classroom> Classrooms { get; set; } = new();
        public List<Term> Terms { get; set; } = new();
        public class ReservationWithColor : Reservation
        {
            public string Color { get; set; } = "#198754";
        }
        public List<ReservationWithColor> MyReservations { get; set; } = new();
        public List<(int GroupId, int ClassroomId, int? TermId, int DayOfWeek, DateTime StartDate, DateTime EndDate, string ClassroomName, string TermName, int Count, string Status)> ReservationGroups { get; set; } = new();
        [BindProperty]
        public string? ResultMessage { get; set; }
        [BindProperty]
        public FeedbackInputModel FeedbackInput { get; set; } = new();
        public List<Feedback> MyFeedbacks { get; set; } = new();
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDateFilter { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDateFilter { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public class FeedbackInputModel
        {
            public int ClassroomId { get; set; }
            public int? TermId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; } = string.Empty;
        }

        private async Task UpdateMyReservationsAsync(string userId)
        {
            Classrooms = await _context.Classrooms.Where(c => c.IsActive).ToListAsync();
            Terms = await _context.Terms.OrderByDescending(t => t.StartDate).ToListAsync();
            var query = _context.Reservations
                .Include(r => r.Classroom)
                .Include(r => r.Term)
                .Include(r => r.User)
                .Where(r => r.UserId == userId && !r.User.IsDeleted);
            if (!string.IsNullOrEmpty(StatusFilter))
                query = query.Where(r => r.Status == StatusFilter);
            if (StartDateFilter.HasValue)
                query = query.Where(r => r.StartDate >= StartDateFilter.Value);
            if (EndDateFilter.HasValue)
                query = query.Where(r => r.EndDate <= EndDateFilter.Value);
            var totalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            var reservations = await query
                .OrderByDescending(r => r.StartDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(r => new ReservationWithColor
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    ClassroomId = r.ClassroomId,
                    Classroom = r.Classroom,
                    TermId = r.TermId,
                    Term = r.Term,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    DayOfWeek = r.DayOfWeek,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    IsHoliday = r.IsHoliday,
                    ConflictReason = r.ConflictReason,
                    Color = "#198754" // renk daha sonra atanacak
                })
                .ToListAsync();
            // Renk ataması
            foreach (var r in reservations)
            {
                var color = "#198754";
                if (r.Status == "Rejected") color = "#8B0000";
                else if (r.Status == "Pending") color = "#ffc107";
                bool isHoliday = await _holidayService.IsHolidayAsync(r.StartTime);
                bool isConflict = await _context.Reservations
                    .Include(r => r.User)
                    .AnyAsync(r =>
                        r.ClassroomId == r.ClassroomId &&
                        r.TermId == r.TermId &&
                        r.DayOfWeek == r.DayOfWeek &&
                        r.Status != "Rejected" &&
                        !r.User.IsDeleted &&
                        ((r.StartTime < r.EndTime && r.EndTime > r.StartTime))
                    );
                if (isHoliday) color = "#adb5bd";
                else if (isConflict) color = "#dc3545";
                r.Color = color;
            }
            MyReservations = reservations;
        }

        private async Task<List<Reservation>> GetAllUserReservationsAsync(string userId)
        {
            return await _context.Reservations
                .Include(r => r.Classroom)
                .Include(r => r.Term)
                .Include(r => r.User)
                .Where(r => r.UserId == userId && !r.User.IsDeleted)
                .ToListAsync();
        }

        public async Task OnGetAsync()
        {
            ResultMessage = null;
            // Page parametresini query string'den manuel olarak oku
            if (Request.Query.ContainsKey("Page"))
            {
                int pageVal;
                if (int.TryParse(Request.Query["Page"], out pageVal))
                    CurrentPage = pageVal;
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                ResultMessage = "User identity not found. Please log in again.";
                return;
            }
            await UpdateMyReservationsAsync(userId!);
            // MyFeedbacks: kullanıcının tüm feedbackleri
            MyFeedbacks = await _context.Feedbacks
                .Include(f => f.Classroom)
                .Include(f => f.Term)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            // ReservationGroups: kullanıcının tüm rezervasyonları üzerinden gruplama
            var allUserReservations = await GetAllUserReservationsAsync(userId!);
            ViewData["AllUserReservations"] = allUserReservations;
            ReservationGroups = allUserReservations
                .GroupBy(r => new { r.ClassroomId, r.TermId, r.DayOfWeek })
                .Select(g => (
                    GroupId: g.Min(x => x.Id),
                    ClassroomId: g.Key.ClassroomId,
                    TermId: g.Key.TermId,
                    DayOfWeek: g.Key.DayOfWeek,
                    StartDate: g.Min(x => x.StartDate),
                    EndDate: g.Max(x => x.EndDate),
                    ClassroomName: g.FirstOrDefault()?.Classroom != null ? g.FirstOrDefault().Classroom.Name : "-",
                    TermName: g.FirstOrDefault()?.Term != null ? g.FirstOrDefault().Term.Name : "-",
                    Count: g.Count(),
                    Status: g.Any(x => x.Status == "Pending") ? "Pending" : (g.All(x => x.Status == "Approved") ? "Approved" : "Mixed")
                ))
                .Where(g => g.Count > 0)
                .OrderByDescending(g => g.StartDate)
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(int ClassroomId, int TermId, int DayOfWeek, DateTime StartDate, DateTime EndDate, string StartTime, string EndTime)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                ResultMessage = "User identity not found. Please log in again.";
                return Page();
            }
            await UpdateMyReservationsAsync(userId!);

            if (ClassroomId == 0 || TermId == 0 || DayOfWeek < 1 || DayOfWeek > 7 || StartDate == default || EndDate == default || string.IsNullOrEmpty(StartTime) || string.IsNullOrEmpty(EndTime))
            {
                ResultMessage = "Reservation information is missing or invalid.";
                return Page();
            }

            var selectedTerm = Terms.FirstOrDefault(t => t.Id == TermId);
            if (selectedTerm == null || StartDate < selectedTerm.StartDate || EndDate > selectedTerm.EndDate)
            {
                ResultMessage = "You can only request reservations within the selected term dates.";
                return Page();
            }

            var classroom = Classrooms.FirstOrDefault(c => c.Id == ClassroomId);
            var term = Terms.FirstOrDefault(t => t.Id == TermId);
            var dayName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName((DayOfWeek)(DayOfWeek % 7));

            // Parse times
            if (!TimeSpan.TryParse(StartTime, out var startTs) || !TimeSpan.TryParse(EndTime, out var endTs))
            {
                ResultMessage = "Invalid time format.";
                return Page();
            }

            var reservations = new List<Reservation>();
            var holidayDates = new List<DateTime>();
            var conflictDates = new List<DateTime>();
            for (var date = StartDate.Date; date <= EndDate.Date; date = date.AddDays(1))
            {
                if ((int)date.DayOfWeek == DayOfWeek % 7)
                {
                    var startDateTime = date.Add(startTs);
                    var endDateTime = date.Add(endTs);
                    // Tatil kontrolü
                    bool isHoliday = await _holidayService.IsHolidayAsync(date);
                    // Çakışma kontrolü
                    bool isConflict = await _context.Reservations
                        .Include(r => r.User)
                        .AnyAsync(r =>
                            r.ClassroomId == ClassroomId &&
                            r.TermId == TermId &&
                            r.DayOfWeek == DayOfWeek &&
                            r.Status != "Rejected" &&
                            !r.User.IsDeleted &&
                            ((r.StartTime < endDateTime && r.EndTime > startDateTime))
                        );
                    if (isHoliday) holidayDates.Add(date);
                    if (isConflict) conflictDates.Add(date);
                    if (!isHoliday && !isConflict)
                    {
                        reservations.Add(new Reservation
                        {
                            UserId = userId!,
                            ClassroomId = ClassroomId,
                            TermId = TermId,
                            DayOfWeek = DayOfWeek,
                            StartDate = StartDate,
                            EndDate = EndDate,
                            StartTime = startDateTime,
                            EndTime = endDateTime,
                            Status = "Pending",
                            IsHoliday = false,
                            ConflictReason = null
                        });
                    }
                }
            }
            if (reservations.Count == 0)
            {
                await _logService.LogAsync(userId!, "Reservation request failed: No valid days", true, $"ClassroomId={ClassroomId}, TermId={TermId}", "Error");
                var msg = "<b>No valid reservation days found. All selected days are either holidays or have conflicts.</b>";
                if (holidayDates.Count > 0)
                    msg += "<br/><b>Public holidays:</b><ul>" + string.Join("", holidayDates.Select(d => $"<li>{d:dd.MM.yyyy} ({d:dddd})</li>")) + "</ul>";
                if (conflictDates.Count > 0)
                {
                    msg += "<br/><b>Conflicts:</b><ul>";
                    foreach (var d in conflictDates)
                    {
                        var startDateTime = d.Add(TimeSpan.Parse(StartTime));
                        var endDateTime = d.Add(TimeSpan.Parse(EndTime));
                        var conflictReservations = await _context.Reservations
                            .Include(r => r.User)
                            .Include(r => r.Classroom)
                            .Where(r => r.ClassroomId == ClassroomId && r.TermId == TermId && r.DayOfWeek == DayOfWeek && r.Status != "Rejected" && r.StartTime < endDateTime && r.EndTime > startDateTime)
                            .ToListAsync();
                        foreach (var c in conflictReservations)
                        {
                            msg += $"<li>{d:dd.MM.yyyy} ({d:dddd}) - <b>{c.Classroom?.Name ?? "-"}</b> {c.StartTime:HH:mm}-{c.EndTime:HH:mm} by {c.User?.Email ?? "-"}</li>";
                        }
                    }
                    msg += "</ul>";
                }
                ResultMessage = msg;

                // E-posta gönderimi (KULLANICI ve ADMIN)
                var user = await _context.Users.FindAsync(userId);
                var userEmail = user?.Email ?? "-";
                // Kullanıcıya e-posta
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var userBody = $@"<div style='font-family:Segoe UI,Arial,sans-serif;'>
                        <h2 style='color:#dc3545;'>Reservation request failed</h2>
                        <p>No valid reservation days found. All selected days are either holidays or have conflicts.</p>
                        <ul>
                            <li><b>Class:</b> {(classroom != null && !string.IsNullOrEmpty(classroom.Name) ? classroom.Name : ClassroomId.ToString())}</li>
                            <li><b>Term:</b> {(term != null && !string.IsNullOrEmpty(term.Name) ? term.Name : TermId.ToString())}</li>
                            <li><b>Day:</b> {dayName}</li>
                            <li><b>Date Range:</b> {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}</li>
                            <li><b>Time:</b> {StartTime} - {EndTime}</li>
                        </ul>
                        <hr/>
                        <div style='font-size:0.9em;color:#888;'>CENG382 Class Reservation System</div>
                    </div>";
                    await _emailService.SendEmailAsync(userEmail, "Reservation request failed", userBody);
                }
                // Admin'e e-posta
                var adminList = await _userManager.GetUsersInRoleAsync("Admin");
                var adminUserObj = adminList.FirstOrDefault(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Email));
                var adminEmailAddress = adminUserObj?.Email ?? _config["AdminEmail"] ?? "admin@admin.com";
                var adminBody = $@"<div style='font-family:Segoe UI,Arial,sans-serif;'>
                    <h2 style='color:#dc3545;'>Instructor Reservation Request Failed</h2>
                    <p><b>{userEmail}</b> instructor requested reservations with the following details, but no valid days were found.</p>
                    <ul>
                        <li><b>Class:</b> {(classroom != null && !string.IsNullOrEmpty(classroom.Name) ? classroom.Name : ClassroomId.ToString())}</li>
                        <li><b>Term:</b> {(term != null && !string.IsNullOrEmpty(term.Name) ? term.Name : TermId.ToString())}</li>
                        <li><b>Day:</b> {dayName}</li>
                        <li><b>Date Range:</b> {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}</li>
                        <li><b>Time:</b> {StartTime} - {EndTime}</li>
                    </ul>
                    <b>Details:</b><br/>{msg}
                    <hr/>
                    <div style='font-size:0.9em;color:#888;'>CENG382 Class Reservation System</div>
                </div>";
                await _emailService.SendEmailAsync(adminEmailAddress, "Instructor Reservation Request Failed", adminBody);
                return Page();
            }
            _context.Reservations.AddRange(reservations);
            await _context.SaveChangesAsync();
            await _logService.LogAsync(userId!, $"Reservation request submitted", false, $"Count={reservations.Count}, ClassroomId={ClassroomId}, TermId={TermId}", "Info");
            if (holidayDates.Count > 0 && conflictDates.Count > 0)
            {
                var msg = $@"<b>Some days were skipped due to holidays and conflicts. Only valid days were reserved.</b><br/>
                <b>Public holidays:</b><ul>{string.Join("", holidayDates.Select(d => $"<li>{d:dd.MM.yyyy} ({d:dddd})</li>"))}</ul>";
                msg += "<b>Conflicts:</b><ul>";
                foreach (var d in conflictDates)
                {
                    var startDateTime = d.Add(TimeSpan.Parse(StartTime));
                    var endDateTime = d.Add(TimeSpan.Parse(EndTime));
                    var conflicts = await _context.Reservations
                        .Include(r => r.User)
                        .Include(r => r.Classroom)
                        .Where(r => r.ClassroomId == ClassroomId && r.TermId == TermId && r.DayOfWeek == DayOfWeek && r.Status != "Rejected" && r.StartTime < endDateTime && r.EndTime > startDateTime)
                        .ToListAsync();
                    foreach (var c in conflicts)
                    {
                        msg += $"<li>{d:dd.MM.yyyy} ({d:dddd}) - <b>{c.Classroom?.Name ?? "-"}</b> {c.StartTime:HH:mm}-{c.EndTime:HH:mm} by {c.User?.Email ?? "-"}</li>";
                    }
                }
                msg += "</ul>";
                ResultMessage = msg;
            }
            else if (holidayDates.Count > 0)
                ResultMessage = $@"<b>Some days were skipped due to holidays. Only valid days were reserved.</b><br/>
                <b>Public holidays:</b><ul>{string.Join("", holidayDates.Select(d => $"<li>{d:dd.MM.yyyy} ({d:dddd})</li>"))}</ul>";
            else if (conflictDates.Count > 0)
            {
                var msg = $@"<b>Some days were skipped due to conflicts. Only valid days were reserved.</b><br/>
                <b>Conflicts:</b><ul>";
                foreach (var d in conflictDates)
                {
                    var startDateTime = d.Add(TimeSpan.Parse(StartTime));
                    var endDateTime = d.Add(TimeSpan.Parse(EndTime));
                    var conflicts = await _context.Reservations
                        .Include(r => r.User)
                        .Include(r => r.Classroom)
                        .Where(r => r.ClassroomId == ClassroomId && r.TermId == TermId && r.DayOfWeek == DayOfWeek && r.Status != "Rejected" && r.StartTime < endDateTime && r.EndTime > startDateTime)
                        .ToListAsync();
                    foreach (var c in conflicts)
                    {
                        msg += $"<li>{d:dd.MM.yyyy} ({d:dddd}) - <b>{c.Classroom?.Name ?? "-"}</b> {c.StartTime:HH:mm}-{c.EndTime:HH:mm} by {c.User?.Email ?? "-"}</li>";
                    }
                }
                msg += "</ul>";
                ResultMessage = msg;
            }
            else
                ResultMessage = "Reservation request(s) submitted!";
            await UpdateMyReservationsAsync(userId!);

            // Admin'e e-posta gönder
            var adminList2 = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUserObj2 = adminList2.FirstOrDefault(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Email));
            var adminEmailAddress2 = adminUserObj2?.Email ?? _config["AdminEmail"] ?? "admin@admin.com";
            var emailBody = $@"
                <b>New Reservation Request</b><br/>
                <b>Instructor:</b> {User.Identity?.Name ?? userId}<br/>
                <b>Classroom:</b> {(classroom != null && !string.IsNullOrEmpty(classroom.Name) ? classroom.Name : ClassroomId.ToString())}<br/>
                <b>Term:</b> {(term != null && !string.IsNullOrEmpty(term.Name) ? term.Name : TermId.ToString())}<br/>
                <b>Day:</b> {dayName}<br/>
                <b>Date Range:</b> {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}<br/>
                <b>Time:</b> {StartTime} - {EndTime}<br/>
            ";
            await _emailService.SendEmailAsync(adminEmailAddress2, "New Reservation Request", emailBody);

            return Page();
        }

        public async Task<IActionResult> OnPostCancelReservationAsync(int reservationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                ResultMessage = "User identity not found. Please log in again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                return Page();
            }
            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);
            if (reservation == null)
            {
                ResultMessage = "Reservation not found.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                await OnGetAsync();
                return Page();
            }
            if (reservation.Status != "Pending")
            {
                ResultMessage = "Only pending reservations can be cancelled.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                await OnGetAsync();
                return Page();
            }
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            ResultMessage = "Reservation cancelled.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return new JsonResult(new { success = true, message = ResultMessage });
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCancelReservationGroupAsync(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                ResultMessage = "User identity not found. Please log in again.";
                return Page();
            }
            // GroupId, gruptaki en küçük reservation Id'si olarak atanıyor
            var groupReservation = await _context.Reservations.Include(r => r.Classroom).Include(r => r.Term).FirstOrDefaultAsync(r => r.Id == groupId && r.UserId == userId);
            if (groupReservation == null)
            {
                ResultMessage = "Reservation group not found.";
                await OnGetAsync();
                return Page();
            }
            // Aynı gruptaki tüm rezervasyonları bul
            var reservations = await _context.Reservations
                .Where(r => r.UserId == userId &&
                            r.ClassroomId == groupReservation.ClassroomId &&
                            r.TermId == groupReservation.TermId &&
                            r.DayOfWeek == groupReservation.DayOfWeek &&
                            r.StartDate == groupReservation.StartDate &&
                            r.EndDate == groupReservation.EndDate &&
                            r.Status == "Pending")
                .ToListAsync();
            if (reservations.Count == 0)
            {
                ResultMessage = "No pending reservations found in this group.";
                await OnGetAsync();
                return Page();
            }
            _context.Reservations.RemoveRange(reservations);
            await _context.SaveChangesAsync();
            ResultMessage = $"{reservations.Count} reservation(s) cancelled.";
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddFeedbackAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                ResultMessage = "User identity not found. Please log in again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                return Page();
            }
            // Kullanıcı bu sınıf ve term için daha önce feedback bırakmış mı?
            bool alreadyLeft = await _context.Feedbacks.AnyAsync(f => f.UserId == userId && f.ClassroomId == FeedbackInput.ClassroomId && f.TermId == FeedbackInput.TermId);
            if (alreadyLeft)
            {
                await _logService.LogAsync(userId!, "Feedback attempt failed: Already left", true, $"ClassroomId={FeedbackInput.ClassroomId}, TermId={FeedbackInput.TermId}", "Warning");
                ResultMessage = "You have already left feedback for this classroom and term.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                await OnGetAsync();
                return Page();
            }
            // Kullanıcının bu sınıfta ve termde rezervasyonu var mı?
            bool hasReservation = await _context.Reservations.AnyAsync(r => r.UserId == userId && r.ClassroomId == FeedbackInput.ClassroomId && r.TermId == FeedbackInput.TermId && r.Status == "Approved");
            if (!hasReservation)
            {
                await _logService.LogAsync(userId!, "Feedback attempt failed: No reservation", true, $"ClassroomId={FeedbackInput.ClassroomId}, TermId={FeedbackInput.TermId}", "Warning");
                ResultMessage = "You can only leave feedback for classrooms you have reserved in the selected term.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                await OnGetAsync();
                return Page();
            }
            var feedback = new Feedback
            {
                UserId = userId!,
                ClassroomId = FeedbackInput.ClassroomId,
                TermId = FeedbackInput.TermId,
                Rating = FeedbackInput.Rating,
                Comment = FeedbackInput.Comment,
                CreatedAt = DateTime.Now
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            await _logService.LogAsync(userId!, "Feedback submitted", false, $"ClassroomId={FeedbackInput.ClassroomId}, TermId={FeedbackInput.TermId}, Rating={FeedbackInput.Rating}", "Info");
            // Admin'e e-posta gönder
            var adminList = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUserObj = adminList.FirstOrDefault(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Email));
            var adminEmailAddress = adminUserObj?.Email ?? _config["AdminEmail"] ?? "admin@admin.com";
            var classroom = await _context.Classrooms.FindAsync(FeedbackInput.ClassroomId);
            var term = FeedbackInput.TermId.HasValue ? await _context.Terms.FindAsync(FeedbackInput.TermId.Value) : null;
            var user = await _context.Users.FindAsync(userId);
            var userEmail = user?.Email ?? userId;
            string emailSendResult = "";
            try
            {
                var feedbackBody = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;max-width:500px;background:#f8f9fa;border-radius:8px;padding:24px;border:1px solid #e0e0e0;'>
                        <h2 style='color:#198754;margin-top:0'>New Feedback Submitted</h2>
                        <table style='width:100%;margin-bottom:16px;'>
                            <tr><td style='font-weight:bold'>Instructor:</td><td>{user?.UserName ?? userId} ({userEmail})</td></tr>
                            <tr><td style='font-weight:bold'>Classroom:</td><td>{(classroom != null && !string.IsNullOrEmpty(classroom.Name) ? classroom.Name : FeedbackInput.ClassroomId.ToString())}</td></tr>
                            <tr><td style='font-weight:bold'>Term:</td><td>{(term != null && !string.IsNullOrEmpty(term.Name) ? term.Name : (FeedbackInput.TermId?.ToString() ?? "-") )}</td></tr>
                            <tr><td style='font-weight:bold'>Rating:</td><td>{FeedbackInput.Rating} / 5</td></tr>
                            <tr><td style='font-weight:bold'>Comment:</td><td>{FeedbackInput.Comment}</td></tr>
                            <tr><td style='font-weight:bold'>Date:</td><td>{DateTime.Now:dd.MM.yyyy HH:mm}</td></tr>
                        </table>
                        <div style='font-size:0.9em;color:#888;border-top:1px solid #e0e0e0;padding-top:8px;'>CENG382 Classroom Reservation System</div>
                    </div>";
                await _emailService.SendEmailAsync(adminEmailAddress, "New Feedback Submitted", feedbackBody);
                emailSendResult = " (Admin'e e-posta iletildi)";
            }
            catch {
                emailSendResult = " (Uyarı: Admin'e e-posta iletilemedi)";
            }
            MyFeedbacks = await _context.Feedbacks
                .Include(f => f.Classroom)
                .Include(f => f.Term)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            await UpdateMyReservationsAsync(userId!);
            ResultMessage = "Feedback submitted successfully." + emailSendResult;
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return new JsonResult(new { success = true, message = ResultMessage });
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditFeedbackAsync(int FeedbackId, int Rating, string Comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                ResultMessage = "User identity not found. Please log in again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                return Page();
            }
            var feedback = await _context.Feedbacks.FirstOrDefaultAsync(f => f.Id == FeedbackId && f.UserId == userId);
            if (feedback == null)
            {
                await _logService.LogAsync(userId!, "Feedback edit failed: Not found", true, $"FeedbackId={FeedbackId}", "Warning");
                ResultMessage = "Feedback not found or you do not have permission to edit it.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ResultMessage });
                await OnGetAsync();
                return Page();
            }
            feedback.Rating = Rating;
            feedback.Comment = Comment;
            // feedback.CreatedAt = feedback.CreatedAt; // Tarihi değiştirme
            _context.Feedbacks.Update(feedback);
            await _context.SaveChangesAsync();
            await _logService.LogAsync(userId!, "Feedback edited", false, $"FeedbackId={FeedbackId}, Rating={Rating}", "Info");
            ResultMessage = "Feedback updated successfully.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return new JsonResult(new { success = true, message = ResultMessage });
            await OnGetAsync();
            return Page();
        }
    }
} 