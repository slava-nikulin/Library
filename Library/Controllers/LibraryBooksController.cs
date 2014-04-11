using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Web.Http;
using System.Web.Mvc;
using Library.Classes;
using Library.Models;
using LibraryDAL;
using Newtonsoft.Json;

namespace Library.Controllers
{
    [System.Web.Http.Authorize]
    public class LibraryBooksController : ApiController
    {
        private LibraryContext db = new LibraryContext();

        // GET api/LibraryBooks
        [System.Web.Mvc.AcceptVerbs("GET", "POST")]
        public ListResult GetBooks(string search, string paging)
        {
            var pagingData = JsonConvert.DeserializeObject<PagingData>(paging);

            var total = db.Books.Count();
            var books = db.Books.ToList();
            if (!string.IsNullOrEmpty(search) && !string.IsNullOrWhiteSpace(search))
            {
                books = books.Where(
                    la =>
                        la.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        la.Author.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                total = books.Count;
            }
            if (!string.IsNullOrEmpty(pagingData.CurrentColumn) && !string.IsNullOrWhiteSpace(pagingData.CurrentColumn))
            {
                if (!string.IsNullOrEmpty(pagingData.SortType) && !string.IsNullOrWhiteSpace(pagingData.SortType))
                {
                    books = pagingData.SortType == "asc"
                        ? books.OrderBy(la => la.GetType().GetProperty(pagingData.CurrentColumn).GetValue(la, null)).ToList()
                        : books.OrderByDescending(la => la.GetType().GetProperty(pagingData.CurrentColumn).GetValue(la, null)).ToList();
                }

            }
            return new ListResult
            {
                ResultLines = books.Skip(pagingData.CurrentPageIndex * pagingData.PageSize).Take(pagingData.PageSize).ToList(),
                TotalLines = total
            };

        }

        // GET api/<LibraryBooks>/5
        public Book GetBook(int id)
        {
            return db.Books.SingleOrDefault(book => book.BookId == id);
        }

        // POST api/<LibraryBooks>

        public void SaveBook(AccountRoomModel model)
        {
            if (model.EditBook.BookId == 0)
            {
                var newBook = new Book
                {
                    Author = model.EditBook.Author,
                    Name = model.EditBook.Name,
                    Description = model.EditBook.Description,
                    ISBN = model.EditBook.Isbn,
                    Status = (int)BookStatus.ReadyForPickup
                };

                db.Books.Add(newBook);
            }
            else
            {
                var bookToEdit = db.Books.SingleOrDefault(book => book.BookId == model.EditBook.BookId);
                if (bookToEdit != null)
                {
                    bookToEdit.Author = model.EditBook.Author;
                    bookToEdit.Description = model.EditBook.Description;
                    bookToEdit.ISBN = model.EditBook.Isbn;
                    bookToEdit.Name = model.EditBook.Name;
                }
            }
            db.SaveChanges();
        }

        // PUT api/<LibraryBooks>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void DeleteBook(int id)
        {
            var bookToDelete = db.Books.SingleOrDefault(book => book.BookId == id);
            if (bookToDelete != null)
            {
                db.Books.Remove(bookToDelete);
            }
            db.SaveChanges();
        }

        public IEnumerable<ReservedBook> GetUserBooks()
        {
            var res = db.LibraryUsers.SelectMany(
                    usr => usr.UserBookCollection.Where(la => DbFunctions.TruncateTime(la.StartDate) <= DateTime.Today
                                                              &&
                                                              (la.Book.Status == (int) BookStatus.Issued ||
                                                               la.Book.Status == (int) BookStatus.ReadyForPickup)))
                    .ToList().Select(la1 => new ReservedBook
                    {
                        UserName = la1.LibraryUser.UserName,
                        BookId = la1.BookId,
                        EndDate = la1.EndDate.ToString("d MMM yyyy"),
                        StartDate = la1.StartDate.ToString("d MMM yyyy"),
                        Name = la1.Book.Name,
                        Status = la1.Book.Status,
                        UserId = la1.LibraryUserId
                    });
            return res;
        }

        public void ChangeBookStatus(int bookId, int newSatus)
        {
            db.Books.Single(book => book.BookId == bookId).Status = newSatus;
            db.SaveChanges();
        }

        public void ReturnTheBook(int bookId, int userId, int newSatus)
        {
            var retBook = db.Books.Single(book => book.BookId == bookId);
            retBook.Status = newSatus;
            retBook.UserBookCollection.Remove(retBook.UserBookCollection.Single(la => la.LibraryUserId == userId && bookId == la.BookId));
            db.SaveChanges();
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}