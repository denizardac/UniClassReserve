using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace UniClassReserve.Data
{
    public class LogService : ILogService
    {
        private readonly AppDbContext _context;
        public LogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string operation, bool isError, string? details = null, string logLevel = "Info")
        {
            var log = new Log
            {
                UserId = userId,
                Operation = operation,
                Timestamp = DateTime.Now,
                IsError = isError,
                Details = details,
                LogLevel = logLevel
            };
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            if (logLevel == "Error")
            {
                try
                {
                    var logDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs");
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    var logPath = Path.Combine(logDir, "error.log");
                    var logLine = $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.UserId}] [{log.Operation}] {log.Details}\n";
                    await File.AppendAllTextAsync(logPath, logLine);
                }
                catch { /* Dosya loglama hatalarını yut */ }
            }
        }

        public async Task LogAsync(string userId, string operation, bool isError, string? details = null)
        {
            await LogAsync(userId, operation, isError, details, "Info");
        }
    }
} 