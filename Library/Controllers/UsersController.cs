using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Security;
using Library.Classes;
using Library.Models;
using LibraryDAL;
using Newtonsoft.Json;
using WebMatrix.WebData;

namespace Library.Controllers
{

    [System.Web.Http.Authorize]
    public class UsersController : ApiController
    {
        private LibraryContext db = new LibraryContext();

        [System.Web.Mvc.AcceptVerbs("GET", "POST")]
        public object GetUsers(string search, string paging)
        {

            var pagingData = JsonConvert.DeserializeObject<PagingData>(paging);
            var users = db.LibraryUsers.ToList().Where(usr => usr.UserName != User.Identity.Name && Roles.IsUserInRole(usr.UserName, "Reader")).ToList();
            var total = users.Count;
            if (!string.IsNullOrEmpty(search) && !string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(
                    la =>
                        la.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        la.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                total = users.Count;
            }
            if (!string.IsNullOrEmpty(pagingData.CurrentColumn) && !string.IsNullOrWhiteSpace(pagingData.CurrentColumn))
            {
                if (!string.IsNullOrEmpty(pagingData.SortType) && !string.IsNullOrWhiteSpace(pagingData.SortType))
                {
                    users = pagingData.SortType == "asc"
                        ? users.OrderBy(la => la.GetType().GetProperty(pagingData.CurrentColumn).GetValue(la, null)).ToList()
                        : users.OrderByDescending(la => la.GetType().GetProperty(pagingData.CurrentColumn).GetValue(la, null)).ToList();
                }

            }
            return new
            {
                ResultLines = users.Skip(pagingData.CurrentPageIndex * pagingData.PageSize).Take(pagingData.PageSize).ToList(),
                TotalLines = total
            };
        }

        public void SaveUser(AccountRoomModel model)
        {
            var password = Membership.GeneratePassword(10, 4);
            WebSecurity.CreateUserAndAccount(model.AddUser.UserName, password, new
            {
                Email = model.AddUser.Email
            });
            Roles.AddUsersToRoles(new[] { model.AddUser.UserName }, new[] { "Reader" });

            //SendMail(model.AddUser.Email, password);
        }

        private static void SendMail(string sendTo, string password)
        {
            try
            {
                var basicAuthenticationInfo = new NetworkCredential(
                    WebConfigurationManager.AppSettings["smtpUsername"],
                    WebConfigurationManager.AppSettings["smtpPassword"]);
                var mySmtpClient = new SmtpClient(WebConfigurationManager.AppSettings["smtpHost"])
                {
                    UseDefaultCredentials = false,
                    Credentials = basicAuthenticationInfo
                };

                var from = new MailAddress("librarian@address.com", "Library admin");
                var to = new MailAddress(sendTo);
                var mail = new MailMessage(@from, to)
                {
                    Subject = "Welcome",
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    Body = password,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = true
                };

                mySmtpClient.Send(mail);
            }

            catch (SmtpException ex)
            {
                throw new ApplicationException
                    ("SmtpException has occured: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DeleteUser(int id)
        {
            var userToDelete = db.LibraryUsers.SingleOrDefault(usr => usr.LibraryUserId == id);
            if (userToDelete != null)
            {
                foreach (var role in Roles.GetRolesForUser(userToDelete.UserName))
                    Roles.RemoveUserFromRole(userToDelete.UserName, role);
                Membership.DeleteUser(userToDelete.UserName, true);
            }
        }

        public IEnumerable<ReservedBook> GetUserInfo(int userId)
        {
            return db.LibraryUsers.Single(usr => usr.LibraryUserId == userId).UserBookCollection.Select(la => new ReservedBook
            {
                BookId = la.BookId,
                Author = la.Book.Author,
                Name = la.Book.Name,
                EndDate = la.EndDate.ToString("d MMM yyyy"),
                StartDate = la.StartDate.ToString("d MMM yyyy"),
                Status = la.Book.Status
            });
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}