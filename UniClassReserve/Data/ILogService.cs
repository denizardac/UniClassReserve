using System.Threading.Tasks;

namespace UniClassReserve.Data
{
    public interface ILogService
    {
        Task LogAsync(string userId, string operation, bool isError, string? details = null, string logLevel = "Info");
        Task LogAsync(string userId, string operation, bool isError, string? details = null);
    }
} 