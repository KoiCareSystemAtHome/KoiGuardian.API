using System.Net.Mail;
using System.Net;
using KoiGuardian.Api.Constants;

namespace KoiGuardian.Api.Helper;

public class SendMail
{
    public static string SendEmail(string to, string subject, string body, string attachFile)
    {
        try
        {
            MailMessage msg = new MailMessage(ConstantValue.emailSender, to, subject, body);

            using (var client = new SmtpClient(ConstantValue.hostEmail, ConstantValue.portEmail))
            {
                client.EnableSsl = true;
                if (!string.IsNullOrEmpty(attachFile))
                {
                    Attachment attachment = new Attachment(attachFile);
                    msg.Attachments.Add(attachment);
                }
                NetworkCredential credential = new NetworkCredential(ConstantValue.emailSender, ConstantValue.passwordSender);
                client.UseDefaultCredentials = false;
                client.Credentials = credential;
                client.Send(msg);
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }
        return "";
    }
}