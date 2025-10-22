using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace ShopOnlineCore.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Chưa cần gửi email thật, chỉ log ra console
            Console.WriteLine($"[Email giả lập] To: {email}, Subject: {subject}");
            Console.WriteLine(htmlMessage);
            return Task.CompletedTask;
        }
    }
}
