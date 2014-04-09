using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using LibraryDAL;
using Microsoft.Web.WebPages.OAuth;
using WebMatrix.WebData;
using Library.Filters;
using Library.Models;

namespace Library.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = string.IsNullOrEmpty(returnUrl) ? Url.Action("Manage") : returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
            {
                return RedirectToLocal(string.IsNullOrEmpty(returnUrl) ? Url.Action("Manage") : returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            return RedirectToAction("Login", "Account");
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (!Roles.RoleExists("Librarian"))
            {
                Roles.CreateRole("Librarian");
            }
            if (!Roles.RoleExists("Reader"))
            {
                Roles.CreateRole("Reader");
            }

            if (!model.IsLibrarian)
            {
                ModelState.Remove("LibrarianPassword");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.IsLibrarian && model.LibrarianPassword != WebConfigurationManager.AppSettings["LibrarianKey"])
                    {
                        ModelState.AddModelError("", "Wrong librarian password");
                        return View(model);
                    }
                    else
                    {
                        using (var con = new LibraryContext())
                        {
                            if (con.LibraryUsers.Any(usr => usr.Email == model.Email))
                            {
                                ModelState.AddModelError("", "User with this email already exists");
                                return View(model);
                            }
                        }
                        WebSecurity.CreateUserAndAccount(model.UserName, model.Password, new
                        {
                            Email = model.Email
                        });
                        Roles.AddUsersToRoles(new[] { model.UserName }, new[] { model.IsLibrarian ? "Librarian" : "Reader" });
                        WebSecurity.Login(model.UserName, model.Password);
                    }

                    return RedirectToAction("Manage", "Account");
                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        /// <summary>
        /// This method just for ajax experience
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult GetLibrarianPassword()
        {
            return Json(new { password = WebConfigurationManager.AppSettings["LibrarianKey"] }, JsonRequestBehavior.AllowGet);
        }

        //
        // POST: /Account/Disassociate

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            // Only disassociate the account if the currently logged in user is the owner
            if (ownerAccount == User.Identity.Name)
            {
                // Use a transaction to prevent the user from deleting their last login credential
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
                {
                    bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
                    if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
                    {
                        OAuthWebSecurity.DeleteAccount(provider, providerUserId);
                        scope.Complete();
                        message = ManageMessageId.RemoveLoginSuccess;
                    }
                }
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage

        public ActionResult Manage() //ManageMessageId? message
        {
            ViewBag.IsLibrarian = User.IsInRole("Librarian");

            var model = new AccountRoomModel();
            using (var con = new LibraryContext())
            {
                model.LibraryUser = con.LibraryUsers.Include("UserBookCollection")
                    .SingleOrDefault(la => la.LibraryUserId == WebSecurity.CurrentUserId);

                model.AllBooks = con.Books.Take(10).ToList();

                model.PasswordModel = new LocalPasswordModel();
                model.EditBook = new CrudBookModel();
            }

            return View(model);

        }

        //
        // POST: /Account/Manage

        [HttpPost]
        public ActionResult ChangePassword(AccountRoomModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                try
                {
                    if (WebSecurity.ChangePassword(User.Identity.Name, model.PasswordModel.OldPassword, model.PasswordModel.NewPassword))
                    {
                        ViewBag.StatusMessage = "Your password has been changed.";
                        return PartialView("_ChangePasswordPartial");
                    }
                }
                catch
                { }
            }
            ViewBag.StatusMessage = "Your password has not been changed.";
            return PartialView("_ChangePasswordPartial");
            //return Json(new { changeMessage = "Password was not changed" }, JsonRequestBehavior.AllowGet);
        }

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion

        [HttpPost]
        public ActionResult SearchBooks(AccountRoomModel model)
        {
            return null;
        }

        [HttpPost]
        public ActionResult AddBook(AccountRoomModel model)
        {
            using (var con = new LibraryContext())
            {
                con.Books.Add(new Book
                {
                    Author = model.EditBook.Author,
                    Name = model.EditBook.Name,
                    Description = model.EditBook.Description,
                    ISBN = model.EditBook.Isbn
                });


                con.SaveChanges();

                model.AllBooks = con.Books.Take(10).ToList();


            }

            return PartialView("Librarian/_EditBooksDatabasePartial", model);
        }
    }
}
