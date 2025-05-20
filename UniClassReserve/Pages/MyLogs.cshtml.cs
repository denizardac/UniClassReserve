using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UniClassReserve.Data;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace UniClassReserve.Pages
{
    [Authorize(Roles = "Instructor")]
    public class MyLogsModel : PageModel
    {
        private readonly AppDbContext _context;
        public MyLogsModel(AppDbContext context)
        {
            _context = context;
        }
        public List<Log> Logs { get; set; } = new();
        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return;
            Logs = await _context.Logs
                .Where(l => l.UserId == userId || l.UserId == userName)
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .ToListAsync();
        }
    }
} 