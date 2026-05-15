using ConaviWeb.Model.Common;
using ConaviWeb.Model.Request;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ConaviWeb.Services
{
    public class MailService : IMailService
    {
        private readonly MailSetting _mailSetting;
        private readonly ILogger<MailService> _logger;
        public MailService(IOptions<MailSetting> mailSetting, ILogger<MailService> logger)
        {
            _mailSetting = mailSetting.Value;
            _logger = logger;
        }
        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            string[] recipients = mailRequest.ToEmail.Split(',');

            using var smtp = new SmtpClient();
            smtp.CheckCertificateRevocation = false;
            smtp.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            smtp.Connect(_mailSetting.Host, _mailSetting.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_mailSetting.Mail, _mailSetting.Password);

            foreach (string recipient in recipients)
            {
                try
                {
                    var email = new MimeMessage();
                    email.Sender = MailboxAddress.Parse(_mailSetting.Mail);
                    email.From.Add(MailboxAddress.Parse(_mailSetting.Mail));
                    email.To.Add(InternetAddress.Parse(recipient.Trim()));
                    email.Bcc.Add(MailboxAddress.Parse("frojas@conavi.gob.mx"));
                    email.Subject = mailRequest.Subject;
                    var builder = new BodyBuilder();
                    if (mailRequest.Attachments != null)
                    {
                        byte[] fileBytes;
                        foreach (var file in mailRequest.Attachments)
                        {
                            if (file.Length > 0)
                            {
                                using (var ms = new MemoryStream())
                                {
                                    file.CopyTo(ms);
                                    fileBytes = ms.ToArray();
                                }
                                builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                            }
                        }
                    }
                    builder.HtmlBody = mailRequest.Body;
                    email.Body = builder.ToMessageBody();
                    await smtp.SendAsync(email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar correo a {Destinatario}", recipient.Trim());
                }
            }

            smtp.Disconnect(true);
        }

    }
}
