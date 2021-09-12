using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CoreBot.Services
{
    public class ExternalServices
    {
        public void sendEmail(string email, string subject, string body)
        {
            SmtpClient smtpClient = new SmtpClient("in-v3.mailjet.com")
            {
                Port = 25,
                Credentials = new NetworkCredential("4432f1ad21458c8e5874ecffc0130ccd", "97273e84e572da757fc178ce04572d4b"),
                EnableSsl = true,
            };
         
            MailMessage mail = new MailMessage("csarun2018@gmail.com", email, subject, body);
            mail.IsBodyHtml = true;
            smtpClient.Send(mail);
        }
    }
}
