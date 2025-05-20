using System.Threading.Tasks;

namespace UniClassReserve.Data
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
} 