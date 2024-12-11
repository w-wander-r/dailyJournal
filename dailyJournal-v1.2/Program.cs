using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MailKit.Net.Imap;
using MailKit;
using MailKit.Search;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Cms;

namespace SendEmailWithGoogleSMTP
{
    class Program
    {
        static void Main()
        {
            string fromMail = "***@gmail.com";
            string fromPassword = "***";
            string toMail = "***@gmail.com";

            CheckForReplies(fromMail, fromPassword, toMail);

            SendEmail(fromMail, fromPassword, toMail);
        }

        static void SendEmail(string fromMail, string fromPassword, string toMail)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(fromMail, "Daily Journal");
            message.Subject = "How was your day?";
            message.To.Add(new MailAddress(toMail));
            message.Body = "<html><body><h1>Describe how you felt during the day by responding to this email</h1></body></html>";
            message.IsBodyHtml = true;

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };

            smtpClient.Send(message);
        }

        static void SendWeeklyRepot(string fromMail, string fromPassword, string toMail, string weeklyReport)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(fromMail, "Daily Journal");
            message.Subject = "Weekly Report";
            message.To.Add(new MailAddress(toMail));
            message.Body = $"<html><body><h1>{weeklyReport}</h1></body></html>";
            message.IsBodyHtml = true;

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };

            smtpClient.Send(message);
        }

        static void CheckForReplies(string fromMail, string fromPassword, string toMail)
        {
            using (var client = new ImapClient())
            {
                client.Connect("imap.gmail.com", 993, true);
                client.Authenticate(fromMail, fromPassword);

                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadWrite);

                var query = SearchQuery.FromContains(toMail).And(SearchQuery.SubjectContains("Re: answer:testv1.2"));
                var uids = inbox.Search(query);
                var lastUid = uids.LastOrDefault(); 

                // 1.2
                if (lastUid != default) 
                {
                    var messageBody = inbox.GetMessage(lastUid); 
                    StoreReplyInDatabase(toMail, messageBody.TextBody, fromMail,fromPassword,toMail);
                    // Console.WriteLine("---------------------------");
                    // Console.WriteLine($"Message to put into DB: {messageBody.TextBody}");
                    // Console.WriteLine("---------------------------");
                }

                client.Disconnect(true);
            }
        }

        static void StoreReplyInDatabase(string email, string replyText, string fromMail, string fromPassword, string toMail)
        {
            string connectionString = "server=localhost;user=root;password=6fbusyXH;database=email_replies";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string insertQuery = "INSERT INTO Replies (Email, ReplyText) VALUES (@Email, @ReplyText)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@ReplyText", replyText);
                    command.ExecuteNonQuery();
                }

                ClearDatabaseAndSendReport(connection, fromMail ,fromPassword,toMail);
            }
        }

        static void ClearDatabaseAndSendReport(MySqlConnection connection, string fromMail, string fromPassword, string toMail)
        {
            string countQuery = "SELECT COUNT(*) FROM Replies";
            using (var command = new MySqlCommand(countQuery, connection))
            {
                int count = Convert.ToInt32(command.ExecuteScalar());
                if (count >= 6)
                {
                    string pythonScriptPath = @"..\dailyJournal-v1.2\AIreport\script.py";
                    string arguments = "";

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "python";
                    startInfo.Arguments = $"{pythonScriptPath} {arguments}";
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true; 

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();

                        string output = process.StandardOutput.ReadToEnd(); 
                        process.WaitForExit(); 
                        string weeklyReport = output;
                        Thread.Sleep(2000);
                        SendWeeklyRepot(fromMail, fromPassword, toMail, weeklyReport);
                        // Console.WriteLine("------------------------------");
                        // Console.WriteLine(weeklyReport);
                        // Console.WriteLine("------------------------------");
                    }

                    
                    string deleteQuery = "DELETE FROM Replies;";
                    using (var deleteCommand = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}