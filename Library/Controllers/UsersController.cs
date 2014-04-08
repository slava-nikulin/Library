using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using Library0.Models;
using LibraryDAL;
using WebMatrix.WebData;

namespace Library0.Controllers
{

    [System.Web.Http.Authorize]
    public class UsersController : ApiController
    {
        [Serializable]
        [DataContract]
        public class ListResult
        {
            [DataMember]
            public int TotalLines { get; set; }
            [DataMember]
            public IEnumerable<LibraryUser> ResultLines { get; set; }
        }

        private LibraryContext db = new LibraryContext();

        // GET api/<controller>
        //TODO: decrease number of parameters by grouping them in classes
        [System.Web.Mvc.AcceptVerbs("GET", "POST")]
        public ListResult GetUsers(string searchKey, int pageind, int pagesize, string currcol, string sort)
        {
            var total = db.LibraryUsers.Count();
            var users = db.LibraryUsers.Where(usr => usr.UserName != User.Identity.Name).ToList();
            if (!string.IsNullOrEmpty(searchKey) && !string.IsNullOrWhiteSpace(searchKey))
            {
                users = users.Where(
                    la =>
                        la.UserName.StartsWith(searchKey, StringComparison.OrdinalIgnoreCase) ||
                        la.Email.StartsWith(searchKey, StringComparison.OrdinalIgnoreCase)).ToList();
                total = users.Count;
            }
            if (!string.IsNullOrEmpty(currcol) && !string.IsNullOrWhiteSpace(currcol))
            {
                if (!string.IsNullOrEmpty(sort) && !string.IsNullOrWhiteSpace(sort))
                {
                    users = sort == "asc"
                        ? users.OrderBy(la => la.GetType().GetProperty(currcol).GetValue(la, null)).ToList()
                        : users.OrderByDescending(la => la.GetType().GetProperty(currcol).GetValue(la, null)).ToList();
                }

            }
            return new ListResult
            {
                ResultLines = users.Skip(pageind * pagesize).Take(pagesize).ToList(),
                TotalLines = total
            };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void SaveUser(AccountRoomModel model)
        {
            var password = Membership.GeneratePassword(10, 4);
            WebSecurity.CreateUserAndAccount(model.AddUser.UserName, password, new
            {
                Email = model.AddUser.Email
            });
            Roles.AddUsersToRoles(new[] { model.AddUser.UserName }, new[] { "Reader" });

            //TODO: send email
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
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

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}