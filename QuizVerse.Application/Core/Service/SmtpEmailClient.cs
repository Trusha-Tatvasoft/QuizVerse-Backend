using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using QuizVerse.Application.Core.Interface;

namespace QuizVerse.Application.Core.Service;

public class SmtpEmailClient : IEmailClient
{
    private SmtpClient? _client;

    #region ConfigureSMTP
    public void Configure(string host, int port, string username, string password, bool enableSsl)
    {
        _client = new SmtpClient
        {
            Host = host,
            Port = port,
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };
    }
    #endregion

    #region Send Mail
    public async Task SendAsync(MailMessage message)
    {
        if (_client == null)
            throw new InvalidOperationException("Email client not configured.");

        await _client.SendMailAsync(message);
    }
    #endregion
}
