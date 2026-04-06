using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using GestaoCulto.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GestaoCulto.Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host = _configuration["Email:Smtp:Host"];
            var fromAddress = _configuration["Email:From:Address"];
            var fromName = _configuration["Email:From:Name"] ?? "Gestao de Culto";
            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];

            if (string.IsNullOrWhiteSpace(host)
                || string.IsNullOrWhiteSpace(fromAddress)
                || string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Configuração SMTP incompleta para envio de e-mail.");
            }

            //var port = LerInt(_configuration["Email:Smtp:Port"], 587);
            //var enableSsl = LerBool(_configuration["Email:Smtp:EnableSsl"], true);

            //using var message = new MailMessage();
            //message.From = new MailAddress(fromAddress, fromName);
            //message.Sender = new MailAddress(username);
            //message.To.Add(new MailAddress(to));
            //message.Subject = subject;
            //message.Body = htmlBody;
            //message.IsBodyHtml = true;

            //using var client = new SmtpClient(host, port)
            //{
            //    UseDefaultCredentials = false,
            //    EnableSsl = enableSsl,
            //    DeliveryMethod = SmtpDeliveryMethod.Network,
            //    Credentials = new NetworkCredential(username, password)
            //};

            // await client.SendMailAsync(message);


            MailMessage mail = new MailMessage()
            {
                From = new MailAddress(username, "Gestão de Culto")
            };

            mail.To.Add(new MailAddress(to));
            mail.Subject = subject;
            mail.Body = htmlBody;
            mail.IsBodyHtml = true;
            mail.Priority = MailPriority.High;

            using (SmtpClient smtp = new SmtpClient(host, 587))
            {
                smtp.Credentials = new NetworkCredential(username, password);
                await smtp.SendMailAsync(mail);
            }
        }

        private static int LerInt(string? valor, int padrao)
        {
            return int.TryParse(valor, out var numero) ? numero : padrao;
        }

        private static bool LerBool(string? valor, bool padrao)
        {
            return bool.TryParse(valor, out var parsed) ? parsed : padrao;
        }
    }
}
