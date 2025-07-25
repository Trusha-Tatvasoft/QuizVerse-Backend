using System.Net.Mail;
using System.Threading.Tasks;

namespace QuizVerse.Application.Core.Interface;
public interface IEmailClient
{
    void Configure(string host, int port, string username, string password, bool enableSsl);
    Task SendAsync(MailMessage message);
}
