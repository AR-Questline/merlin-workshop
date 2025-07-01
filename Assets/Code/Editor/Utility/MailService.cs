using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace Awaken.TG.Editor.Utility {
    public static class MailService {
        const string Emails = "k.socha@awakenrealms.com, d.majchrzak@awakenrealms.com, p.wicher@awakenrealms.com, k.maslanka@awakenrealms.com, m.sabat@awakenrealms.com";
       
        public static void SendMail(string title, string message, params string[] attachments) {
            var userName = new MailAddress("Ziemniak.TG.AR@gmail.com");
           
            using var smtpClient = new SmtpClient("smtp.gmail.com") {
                EnableSsl = true,
                Port = 587,
                Credentials = new NetworkCredential("Ziemniak.TG.AR@gmail.com", "ARZiemniak1234"),
            };
 
            var mailMessage = new MailMessage
            {
                From = userName,
                Subject = title,
                Body = message,
            };

            foreach (var attachment in attachments) {
                if (!string.IsNullOrWhiteSpace(attachment)) {
                    mailMessage.Attachments.Add(new Attachment(attachment, MediaTypeNames.Text.Plain));
                }
            }

            AssignTo(mailMessage, true, new string[0]);
 
            smtpClient.Send(mailMessage);
        }
       
        static void AssignTo(MailMessage message, bool withProgrammers, string[] others)
        {
            var programmersEmails = Emails.Split(',').Select(e => e.Trim()).Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
            HashSet<string> recipient = new HashSet<string>(others);
            if (withProgrammers)
            {
                recipient.UnionWith(programmersEmails);
            }
 
            var mails = message.To;
            foreach (var mail in recipient)
            {
                mails.Add(mail);
            }
        }
    }
}