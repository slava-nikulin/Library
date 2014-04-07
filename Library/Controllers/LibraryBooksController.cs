using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Web.Http;
using System.Web.Mvc;
using Library0.Models;
using LibraryDAL;
using Newtonsoft.Json;

namespace Library0.Controllers
{
    [System.Web.Http.Authorize]
    public class LibraryBooksController : ApiController
    {
        [Serializable]
        [DataContract]
        public class ListResult
        {
            [DataMember]
            public int TotalLines { get; set; }
            [DataMember]
            public IEnumerable<Book> ResultBooks { get; set; }
        }


        private LibraryContext db = new LibraryContext();

        // GET api/LibraryBooks
        [System.Web.Mvc.AcceptVerbs("GET", "POST")]
        public ListResult GetBooks(string searchKey, int pageind, int pagesize, string currcol, string sort)
        {
            var total = db.Books.Count();
            var books = db.Books.ToList();
            if (!string.IsNullOrEmpty(searchKey) && !string.IsNullOrWhiteSpace(searchKey))
            {
                books = books.Where(
                    la =>
                        la.Name.StartsWith(searchKey, StringComparison.OrdinalIgnoreCase) ||
                        la.Author.StartsWith(searchKey, StringComparison.OrdinalIgnoreCase)).ToList();
                total = books.Count;
            }
            if (!string.IsNullOrEmpty(currcol) && !string.IsNullOrWhiteSpace(currcol))
            {
                if (!string.IsNullOrEmpty(sort) && !string.IsNullOrWhiteSpace(sort))
                {
                    books = sort == "asc"
                        ? books.OrderBy(la => la.GetType().GetProperty(currcol).GetValue(la, null)).ToList()
                        : books.OrderByDescending(la => la.GetType().GetProperty(currcol).GetValue(la, null)).ToList();
                }

            }
            return new ListResult
            {
                ResultBooks = books.Skip(pageind * pagesize).Take(pagesize).ToList(),
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
                    ISBN = model.EditBook.Isbn
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

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}