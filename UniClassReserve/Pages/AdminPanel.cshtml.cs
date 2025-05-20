using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Linq;
using UniClassReserve.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UniClassReserve.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminPanelModel : PageModel
    {
        private readonly UserManager<Data.ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IHolidayService _holidayService;
        private readonly ILogService _logService;
        private readonly IEmailService _emailService;

        public AdminPanelModel(UserManager<Data.ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context, IHolidayService holidayService, ILogService logService, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _holidayService = holidayService;
            _logService = logService;
            _emailService = emailService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;
        [BindProperty]
        public string Password { get; set; } = string.Empty;
        [BindProperty]
        public string Role { get; set; } = string.Empty;
        public string? ResultMessage { get; set; }

        // Term (Dönem) yönetimi için
        [BindProperty]
        public TermInputModel TermInput { get; set; } = new();
        public List<Term> Terms { get; set; } = new();
        public class TermInputModel
        {
            public int Id { get; set; }
            [Required]
            public string Name { get; set; } = string.Empty;
            [Required]
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; }
            [Required]
            [DataType(DataType.Date)]
            public DateTime EndDate { get; set; }
        }

        // Classroom yönetimi için
        [BindProperty]
        public ClassroomInputModel ClassroomInput { get; set; } = new();
        public List<Classroom> Classrooms { get; set; } = new();
        public class ClassroomInputModel
        {
            public int Id { get; set; }
            [Required]
            public string Name { get; set; } = string.Empty;
            [Required]
            [Range(1, 1000, ErrorMessage = "Capacity must be at least 1")]
            public int Capacity { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; } = true;
        }

        public class UserWithRolesViewModel
        {
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Roles { get; set; } = string.Empty;
        }

        public List<UserWithRolesViewModel> UsersWithRoles { get; set; } = new();

        public List<Reservation> AllReservations { get; set; } = new();

        public List<(int GroupId, int ClassroomId, int? TermId, int DayOfWeek, DateTime StartDate, DateTime EndDate, string? UserEmail, string ClassroomName, string TermName, int Count, string Status)> ReservationAdminGroups { get; set; } = new();

        public List<Feedback> AllFeedbacks { get; set; } = new();

        [BindProperty]
        public string TestEmailTo { get; set; } = string.Empty;
        [BindProperty]
        public string TestEmailMessage { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? FeedbackRatingFilter { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FeedbackSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FeedbackPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int? FeedbackRating { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? FeedbackStartDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? FeedbackEndDate { get; set; }
        public int FeedbackPageSize { get; set; } = 20;
        public int FeedbackTotalPages { get; set; }

        public class ConflictInfo
        {
            public Reservation Reservation { get; set; } = null!;
            public Reservation ConflictingReservation { get; set; } = null!;
            public DateTime OverlapStart { get; set; }
            public DateTime OverlapEnd { get; set; }
        }
        public List<ConflictInfo> ConflictingReservations { get; set; } = new();

        // LOG GÖRÜNTÜLEME EKLENTİSİ
        [BindProperty(SupportsGet = true)]
        public string? LogLevel { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? LogUserId { get; set; }
        [BindProperty(SupportsGet = true)]
        public int LogPage { get; set; } = 1;
        public int LogPageSize { get; set; } = 20;
        public int LogTotalPages { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? LogStartDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? LogEndDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? LogSearch { get; set; }
        public List<Log> Logs { get; set; } = new();

        // Log tablosu için view model
        public class LogViewModel
        {
            public string Date { get; set; } = "";
            public string UserEmail { get; set; } = "";
            public string LogLevel { get; set; } = "";
            public string Operation { get; set; } = "";
            public string Details { get; set; } = "";
        }
        public List<LogViewModel> LogsViewModel { get; set; } = new();

        [BindProperty]
        public string BulkEmailTo { get; set; } = string.Empty;
        [BindProperty]
        public string BulkEmailMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            UsersWithRoles = new List<UserWithRolesViewModel>();
            foreach (var u in _userManager.Users.Where(u => !u.IsDeleted))
            {
                var roles = await _userManager.GetRolesAsync(u);
                UsersWithRoles.Add(new UserWithRolesViewModel
                {
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    Roles = string.Join(", ", roles)
                });
            }
            Terms = _context.Terms.OrderByDescending(t => t.StartDate).ToList();
            Classrooms = _context.Classrooms.OrderBy(c => c.Name).ToList();
            // Reservation sorgusu (sadece aktif kullanıcılar)
            var reservationsQuery = _context.Reservations
                .Include(r => r.Classroom)
                .Include(r => r.Term)
                .Include(r => r.User)
                .Where(r => r.User != null && !r.User.IsDeleted)
                .OrderByDescending(r => r.StartDate);
            AllReservations = await reservationsQuery.ToListAsync();
            // Admin için batch gruplama (user, classroom, term, day, start/end, status) - sadece aktif kullanıcılar
            ReservationAdminGroups = AllReservations
                .GroupBy(r => new { r.UserId, r.ClassroomId, r.TermId, r.DayOfWeek, r.StartDate, r.EndDate, r.Status })
                .Select(g => (
                    GroupId: g.Min(x => x.Id),
                    g.Key.ClassroomId,
                    g.Key.TermId,
                    g.Key.DayOfWeek,
                    g.Key.StartDate,
                    g.Key.EndDate,
                    UserEmail: g.FirstOrDefault()?.User != null ? g.FirstOrDefault().User.Email : null,
                    ClassroomName: g.FirstOrDefault()?.Classroom != null ? g.FirstOrDefault().Classroom.Name : "-",
                    TermName: g.FirstOrDefault()?.Term != null ? g.FirstOrDefault().Term.Name : "-",
                    Count: g.Count(),
                    Status: g.Key.Status))
                .OrderByDescending(g => g.StartDate)
                .ToList();
            // AllFeedbacks sorgusu (sadece aktif kullanıcılar)
            var feedbacksQuery = _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Classroom)
                .Include(f => f.Term)
                .Where(f => f.User != null && !f.User.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .AsQueryable();
            if (FeedbackRating.HasValue)
                feedbacksQuery = feedbacksQuery.Where(f => f.Rating == FeedbackRating.Value);
            if (FeedbackStartDate.HasValue)
                feedbacksQuery = feedbacksQuery.Where(f => f.CreatedAt >= FeedbackStartDate.Value);
            if (FeedbackEndDate.HasValue)
                feedbacksQuery = feedbacksQuery.Where(f => f.CreatedAt <= FeedbackEndDate.Value);
            if (FeedbackRatingFilter.HasValue)
                feedbacksQuery = feedbacksQuery.Where(f => f.Rating == FeedbackRatingFilter.Value);
            if (!string.IsNullOrWhiteSpace(FeedbackSearch))
                feedbacksQuery = feedbacksQuery.Where(f => (f.Comment != null && f.Comment.ToLower().Contains(FeedbackSearch.ToLower())) || (f.User != null && f.User.Email != null && f.User.Email.ToLower().Contains(FeedbackSearch.ToLower())));
            var feedbackTotalCount = await feedbacksQuery.CountAsync();
            FeedbackTotalPages = (int)Math.Ceiling(feedbackTotalCount / (double)FeedbackPageSize);
            AllFeedbacks = await feedbacksQuery
                .Skip((FeedbackPage - 1) * FeedbackPageSize)
                .Take(FeedbackPageSize)
                .Select(f => new Feedback {
                    Id = f.Id,
                    UserId = f.UserId,
                    User = f.User,
                    ClassroomId = f.ClassroomId,
                    Classroom = f.Classroom,
                    TermId = f.TermId,
                    Term = f.Term,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt,
                    IsRead = f.IsRead
                })
                .ToListAsync();
            // Çakışan rezervasyonları bul
            ConflictingReservations = new List<ConflictInfo>();
            var approvedOrPending = AllReservations.Where(r => r.Status != "Rejected").ToList();
            for (int i = 0; i < approvedOrPending.Count; i++)
            {
                var r1 = approvedOrPending[i];
                for (int j = i + 1; j < approvedOrPending.Count; j++)
                {
                    var r2 = approvedOrPending[j];
                    if (r1.Id == r2.Id) continue;
                    if (r1.ClassroomId == r2.ClassroomId && r1.TermId == r2.TermId && r1.DayOfWeek == r2.DayOfWeek)
                    {
                        if (r1.StartTime < r2.EndTime && r1.EndTime > r2.StartTime)
                        {
                            var overlapStart = r1.StartTime > r2.StartTime ? r1.StartTime : r2.StartTime;
                            var overlapEnd = r1.EndTime < r2.EndTime ? r1.EndTime : r2.EndTime;
                            ConflictingReservations.Add(new ConflictInfo
                            {
                                Reservation = r1,
                                ConflictingReservation = r2,
                                OverlapStart = overlapStart,
                                OverlapEnd = overlapEnd
                            });
                        }
                    }
                }
            }
            // LOG FİLTRELEME VE SAYFALAMA
            var logsQuery = _context.Logs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(LogLevel))
                logsQuery = logsQuery.Where(l => l.LogLevel == LogLevel);
            if (!string.IsNullOrWhiteSpace(LogUserId))
                logsQuery = logsQuery.Where(l => l.UserId == LogUserId);
            if (LogStartDate.HasValue)
                logsQuery = logsQuery.Where(l => l.Timestamp >= LogStartDate.Value);
            if (LogEndDate.HasValue)
                logsQuery = logsQuery.Where(l => l.Timestamp <= LogEndDate.Value);
            if (!string.IsNullOrWhiteSpace(LogSearch))
                logsQuery = logsQuery.Where(l => l.Operation.Contains(LogSearch) || (l.Details != null && l.Details.Contains(LogSearch)));
            var logCount = await logsQuery.CountAsync();
            LogTotalPages = (int)Math.Ceiling(logCount / (double)LogPageSize);
            var logs = await logsQuery.OrderByDescending(l => l.Timestamp)
                .Skip((LogPage - 1) * LogPageSize)
                .Take(LogPageSize)
                .ToListAsync();
            // UserId -> Email eşlemesi
            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var userDict = _userManager.Users.Where(u => userIds.Contains(u.Id) || (u.Email != null && userIds.Contains(u.Email))).ToDictionary(u => u.Id, u => u.Email ?? u.Id);
            LogsViewModel = logs.Select(l => new LogViewModel
            {
                Date = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                UserEmail = userDict.ContainsKey(l.UserId) ? userDict[l.UserId] : l.UserId,
                LogLevel = l.LogLevel,
                Operation = l.Operation,
                Details = FormatDetails(l.Details)
            }).ToList();
            ModelState.Clear();
        }

        public async Task<IActionResult> OnPostAsync(string Email, string Password, string Role)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Role))
            {
                ResultMessage = "Please fill in all required fields.";
                await OnGetAsync();
                return Page();
            }
            // Şifre boşsa otomatik güçlü şifre üret
            string passwordToUse = Password;
            if (string.IsNullOrWhiteSpace(Password))
            {
                passwordToUse = GenerateStrongPassword();
            }
            // Kullanıcıyı oluştur (UserName = Email)
            var user = new Data.ApplicationUser { UserName = Email, Email = Email };
            var result = await _userManager.CreateAsync(user, passwordToUse);
            if (!result.Succeeded)
            {
                await _logService.LogAsync(User.Identity?.Name ?? "Admin", "User creation failed", true, string.Join(" ", result.Errors.Select(e => e.Description)), "Error");
                ResultMessage = "User could not be created. " + string.Join(" ", result.Errors.Select(e => e.Description));
                await OnGetAsync();
                return Page();
            }
            // Önce tüm rollerini sil, sonra yeni rol ata
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
                await _userManager.RemoveFromRolesAsync(user, roles);
            if (!await _roleManager.RoleExistsAsync(Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(Role));
            }
            await _userManager.AddToRoleAsync(user, Role);
            await _logService.LogAsync(User.Identity?.Name ?? "Admin", "User created", false, $"Email={Email}, Role={Role}", "Info");
            ResultMessage = $"User has been added successfully and assigned the '{Role}' role.";
            // Kullanıcıya hoş geldin e-postası ve şifre gönder
            try
            {
                var forgotUrl = Url.Page(
                    "/Account/ForgotPassword",
                    pageHandler: null,
                    values: new { area = "Identity" },
                    protocol: Request.Scheme);
                var body = $"<p>Welcome! Your account has been created.<br/>Email: {Email}<br/>Password: {passwordToUse}</p>" +
                           $"<p>For security, please <a href='{forgotUrl}'>reset your password here</a> after your first login.</p>";
                await _emailService.SendEmailAsync(Email, "Welcome to Classroom Reservation System", body);
            }
            catch { }
            await OnGetAsync();
            return Page();
        }

        // Güçlü şifre üretici
        private string GenerateStrongPassword()
        {
            var upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var lower = "abcdefghijkmnopqrstuvwxyz";
            var digits = "23456789";
            var specials = "!@$?_-";
            var all = upper + lower + digits + specials;
            var rand = new Random();
            var password = new List<char>
            {
                upper[rand.Next(upper.Length)],
                lower[rand.Next(lower.Length)],
                digits[rand.Next(digits.Length)],
                specials[rand.Next(specials.Length)]
            };
            for (int i = 4; i < 12; i++)
                password.Add(all[rand.Next(all.Length)]);
            // Karıştır
            return new string(password.OrderBy(x => rand.Next()).ToArray());
        }

        public async Task<IActionResult> OnPostAddTermAsync(string Name, DateTime StartDate, DateTime EndDate)
        {
            if (string.IsNullOrWhiteSpace(Name) || StartDate == default || EndDate == default)
            {
                ResultMessage = "Term information is missing or invalid.";
                await OnGetAsync();
                return Page();
            }
            var term = new Term
            {
                Name = Name,
                StartDate = StartDate,
                EndDate = EndDate
            };
            _context.Terms.Add(term);
            await _context.SaveChangesAsync();
            await _logService.LogAsync(User.Identity?.Name ?? "Admin", "Term added", false, $"Name={Name}, Start={StartDate}, End={EndDate}", "Info");
            ResultMessage = "Term has been added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditTermAsync(int Id, string Name, DateTime StartDate, DateTime EndDate)
        {
            if (string.IsNullOrWhiteSpace(Name) || StartDate == default || EndDate == default)
            {
                ResultMessage = "Term information is missing or invalid.";
                await OnGetAsync();
                return Page();
            }
            var term = await _context.Terms.FindAsync(Id);
            if (term == null)
            {
                ResultMessage = "Term not found.";
                await OnGetAsync();
                return Page();
            }
            term.Name = Name;
            term.StartDate = StartDate;
            term.EndDate = EndDate;
            await _context.SaveChangesAsync();
            ResultMessage = "Term has been updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteTermAsync(int id)
        {
            var term = await _context.Terms.FindAsync(id);
            if (term == null)
            {
                ResultMessage = "Term not found.";
                await OnGetAsync();
                return Page();
            }
            _context.Terms.Remove(term);
            await _context.SaveChangesAsync();
            ResultMessage = "Term has been deleted successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ResultMessage = "User not found.";
                await OnGetAsync();
                return Page();
            }
            user.IsDeleted = true;
            await _userManager.UpdateAsync(user);
            ResultMessage = "User has been deleted.";
            await OnGetAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddClassroomAsync(string Name, int Capacity, string? Description, bool IsActive)
        {
            if (string.IsNullOrWhiteSpace(Name) || Capacity < 1)
            {
                ResultMessage = "Classroom information is missing or invalid. Name and Capacity are required.";
                await OnGetAsync();
                return Page();
            }
            var classroom = new Classroom
            {
                Name = Name,
                Capacity = Capacity,
                Description = Description,
                IsActive = IsActive
            };
            _context.Classrooms.Add(classroom);
            await _context.SaveChangesAsync();
            ResultMessage = "Classroom has been added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditClassroomAsync(int Id, string Name, int Capacity, string? Description, bool IsActive)
        {
            if (string.IsNullOrWhiteSpace(Name) || Capacity < 1)
            {
                ResultMessage = "Classroom information is missing or invalid. Name and Capacity are required.";
                await OnGetAsync();
                return Page();
            }
            var classroom = await _context.Classrooms.FindAsync(Id);
            if (classroom == null)
            {
                ResultMessage = "Classroom not found.";
                await OnGetAsync();
                return Page();
            }
            classroom.Name = Name;
            classroom.Capacity = Capacity;
            classroom.Description = Description;
            classroom.IsActive = IsActive;
            await _context.SaveChangesAsync();
            ResultMessage = "Classroom has been updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteClassroomAsync(int id)
        {
            var classroom = await _context.Classrooms.FindAsync(id);
            if (classroom == null)
            {
                ResultMessage = "Classroom not found.";
                await OnGetAsync();
                return Page();
            }
            _context.Classrooms.Remove(classroom);
            await _context.SaveChangesAsync();
            ResultMessage = "Classroom has been deleted successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveReservationAsync(int id)
        {
            var reservation = await _context.Reservations.Include(r => r.Classroom).Include(r => r.Term).Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null)
            {
                await _logService.LogAsync(User.Identity?.Name ?? "Admin", "Reservation approval failed: Not found", true, $"ReservationId={id}", "Error");
                ResultMessage = "Reservation not found.";
                await OnGetAsync();
                return Page();
            }
            // Tatil ve çakışma kontrolü
            bool isHoliday = await _holidayService.IsHolidayAsync(reservation.StartTime);
            bool isConflict = await _context.Reservations.AnyAsync(r =>
                r.Id != reservation.Id &&
                r.ClassroomId == reservation.ClassroomId &&
                r.TermId == reservation.TermId &&
                r.DayOfWeek == reservation.DayOfWeek &&
                r.Status == "Approved" &&
                ((r.StartTime < reservation.EndTime && r.EndTime > reservation.StartTime))
            );
            if (isHoliday || isConflict)
            {
                await _logService.LogAsync(User.Identity?.Name ?? "Admin", "Reservation approval failed: Conflict/Holiday", true, $"ReservationId={id}", "Warning");
                ResultMessage = $"Cannot approve: {(isHoliday ? "Date is a public holiday. " : "")}{(isConflict ? "Time conflict with another approved reservation." : "")}";
                // Tatil veya çakışma nedeniyle onaylanamayan rezervasyon için kullanıcıya e-posta gönder
                if (reservation.User != null && reservation.User.Email != null)
                {
                    string subject = "Reservation Could Not Be Approved";
                    string reason = (isHoliday ? "The requested date is a public holiday. " : "") + (isConflict ? "There is a time conflict with another approved reservation." : "");
                    string body = $"Your reservation for {(reservation.Classroom != null ? reservation.Classroom.Name : "-")} on {reservation.StartTime:dd.MM.yyyy HH:mm} - {reservation.EndTime:HH:mm} could not be approved.<br/><b>Reason:</b> {reason}";
                    await _emailService.SendEmailAsync(reservation.User.Email, subject, body);
                }
                await OnGetAsync();
                return Page();
            }
            reservation.Status = "Approved";
            await _context.SaveChangesAsync();
            await _logService.LogAsync(User.Identity?.Name ?? "Admin", "Reservation approved", false, $"ReservationId={id}", "Info");
            // E-posta gönder
            if (reservation.User != null && reservation.User.Email != null)
            {
                string subject = "Reservation Approved";
                string body = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;'>
                        <h2 style='color:#198754;'>Reservation Approved</h2>
                        <p>Your reservation has been <b style='color:#198754;'>approved</b>!</p>
                        <ul>
                            <li><b>Classroom:</b> {(reservation.Classroom != null ? reservation.Classroom.Name : "-")}</li>
                            <li><b>Date:</b> {reservation.StartTime:dd.MM.yyyy} ({reservation.StartTime:dddd})</li>
                            <li><b>Time:</b> {reservation.StartTime:HH:mm} - {reservation.EndTime:HH:mm}</li>
                            <li><b>Term:</b> {(reservation.Term != null ? reservation.Term.Name : "-")}</li>
                        </ul>
                        {(string.IsNullOrWhiteSpace(reservation.AdminNote) ? "" : $"<p><b>Admin Note:</b> {reservation.AdminNote}</p>")}
                        <hr/>
                        <div style='font-size:0.9em;color:#888;'>CENG382 Classroom Reservation System</div>
                    </div>";
                await _emailService.SendEmailAsync(reservation.User.Email, subject, body);
            }
            ResultMessage = "Reservation approved.";
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRejectReservationAsync(int id)
        {
            var reservation = await _context.Reservations.Include(r => r.User).Include(r => r.Classroom).Include(r => r.Term).FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null)
            {
                await _logService.LogAsync(User.Identity?.Name ?? "Admin", "Reservation rejection failed: Not found", true, $"ReservationId={id}", "Error");
                ResultMessage = "Reservation not found.";
                await OnGetAsync();
                return Page();
            }
            reservation.Status = "Rejected";
            await _context.SaveChangesAsync();
            await _logService.LogAsync(User.Identity?.Name ?? "Admin", "Reservation rejected", false, $"ReservationId={id}", "Info");
            // E-posta gönder
            if (reservation.User != null && reservation.User.Email != null)
            {
                string subject = "Reservation Rejected";
                string body = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;'>
                        <h2 style='color:#dc3545;'>Reservation Rejected</h2>
                        <p>Your reservation has been <b style='color:#dc3545;'>rejected</b>.</p>
                        <ul>
                            <li><b>Classroom:</b> {(reservation.Classroom != null ? reservation.Classroom.Name : "-")}</li>
                            <li><b>Date:</b> {reservation.StartTime:dd.MM.yyyy} ({reservation.StartTime:dddd})</li>
                            <li><b>Time:</b> {reservation.StartTime:HH:mm} - {reservation.EndTime:HH:mm}</li>
                            <li><b>Term:</b> {(reservation.Term != null ? reservation.Term.Name : "-")}</li>
                        </ul>
                        {(string.IsNullOrWhiteSpace(reservation.AdminNote) ? "" : $"<p><b>Admin Note:</b> {reservation.AdminNote}</p>")}
                        <hr/>
                        <div style='font-size:0.9em;color:#888;'>CENG382 Classroom Reservation System</div>
                    </div>";
                await _emailService.SendEmailAsync(reservation.User.Email, subject, body);
            }
            ResultMessage = "Reservation rejected.";
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveReservationGroupAsync(int groupId)
        {
            var groupReservation = await _context.Reservations.Include(r => r.User).Include(r => r.Classroom).Include(r => r.Term).FirstOrDefaultAsync(r => r.Id == groupId);
            if (groupReservation == null)
            {
                ResultMessage = "Reservation group not found.";
                await OnGetAsync();
                return Page();
            }
            var reservations = await _context.Reservations
                .Where(r => r.UserId == groupReservation.UserId &&
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
            // Tatil ve çakışma kontrolü (her rezervasyon için)
            var holidayDates = new List<DateTime>();
            var conflictDates = new List<DateTime>();
            foreach (var r in reservations)
            {
                bool isHoliday = await _holidayService.IsHolidayAsync(r.StartTime);
                bool isConflict = await _context.Reservations.AnyAsync(x =>
                    x.Id != r.Id &&
                    x.ClassroomId == r.ClassroomId &&
                    x.TermId == r.TermId &&
                    x.DayOfWeek == r.DayOfWeek &&
                    x.Status == "Approved" &&
                    ((x.StartTime < r.EndTime && x.EndTime > r.StartTime))
                );
                if (isHoliday) holidayDates.Add(r.StartTime);
                if (isConflict) conflictDates.Add(r.StartTime);
            }
            if (holidayDates.Count > 0 || conflictDates.Count > 0)
            {
                ResultMessage = $"Cannot approve group: " +
                    (holidayDates.Count > 0 ? $"Holiday(s): {string.Join(", ", holidayDates.Select(d => d.ToShortDateString()))}. " : "") +
                    (conflictDates.Count > 0 ? $"Conflict(s): {string.Join(", ", conflictDates.Select(d => d.ToShortDateString()))}." : "");
                await OnGetAsync();
                return Page();
            }
            foreach (var r in reservations) r.Status = "Approved";
            await _context.SaveChangesAsync();
            ResultMessage = $"{reservations.Count} reservation(s) approved.";
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRejectReservationGroupAsync(int groupId)
        {
            var groupReservation = await _context.Reservations.Include(r => r.User).Include(r => r.Classroom).Include(r => r.Term).FirstOrDefaultAsync(r => r.Id == groupId);
            if (groupReservation == null)
            {
                ResultMessage = "Reservation group not found.";
                await OnGetAsync();
                return Page();
            }
            var reservations = await _context.Reservations
                .Where(r => r.UserId == groupReservation.UserId &&
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
            foreach (var r in reservations) r.Status = "Rejected";
            await _context.SaveChangesAsync();
            ResultMessage = $"{reservations.Count} reservation(s) rejected.";
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostTestEmailAsync(string TestEmailTo, string TestEmailMessage)
        {
            if (string.IsNullOrWhiteSpace(TestEmailTo))
            {
                ResultMessage = "Please enter a valid email address.";
                await OnGetAsync();
                return Page();
            }
            try
            {
                await _emailService.SendEmailAsync(TestEmailTo, "Test Email from Admin Panel", TestEmailMessage ?? "This is a test email.");
                ResultMessage = $"Test email sent to {TestEmailTo}.";
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("SYSTEM", "Test email send failed", true, ex.Message, "Error");
                ResultMessage = "Test email could not be sent. Please check your email settings.";
            }
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostMarkFeedbackReadAsync(int id, bool IsRead)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                ResultMessage = "Feedback not found.";
                await OnGetAsync();
                return Page();
            }
            feedback.IsRead = IsRead;
            await _context.SaveChangesAsync();
            ResultMessage = IsRead ? "Feedback marked as read." : "Feedback marked as unread.";
            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteFeedbackAsync(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                ResultMessage = "Feedback not found.";
                await OnGetAsync();
                return Page();
            }
            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
            ResultMessage = "Feedback has been deleted.";
            await OnGetAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBulkEmailAsync()
        {
            if (string.IsNullOrWhiteSpace(BulkEmailTo) || string.IsNullOrWhiteSpace(BulkEmailMessage))
            {
                ResultMessage = "Please select at least one instructor and enter a message.";
                await OnGetAsync();
                return Page();
            }
            var emails = BulkEmailTo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
            int success = 0, fail = 0;
            foreach (var email in emails)
            {
                try
                {
                    await _emailService.SendEmailAsync(email, "[Admin] Announcement", BulkEmailMessage);
                    success++;
                }
                catch
                {
                    fail++;
                }
            }
            await _logService.LogAsync(User.Identity?.Name ?? "Admin", $"Bulk email sent to {success} instructors, {fail} failed.", fail > 0, $"Emails: {string.Join(", ", emails)}");
            ResultMessage = $"Bulk email sent to {success} instructor(s).{(fail > 0 ? $" {fail} failed." : "")}";
            await OnGetAsync();
            return Page();
        }

        // Log details'ı insan okunur hale getirir
        private string FormatDetails(string? details)
        {
            if (string.IsNullOrWhiteSpace(details)) return "-";
            // JSON ise prettify et
            if ((details.Trim().StartsWith("{") && details.Trim().EndsWith("}")) || (details.Trim().StartsWith("[") && details.Trim().EndsWith("]")))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(details);
                    return JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                }
                catch { }
            }
            // Virgül veya ; ile ayrılmışsa satır satır göster
            if (details.Contains(","))
                return string.Join("<br>", details.Split(",").Select(x => x.Trim()));
            if (details.Contains(";"))
                return string.Join("<br>", details.Split(';').Select(x => x.Trim()));
            return details;
        }
    }
} 