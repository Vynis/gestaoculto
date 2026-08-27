using System.Threading.Tasks;

namespace GestaoCulto.Application.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }
}
