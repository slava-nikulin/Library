using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Script.Serialization;
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

        [System.Web.Mvc.AcceptVerbs("GET", "POST")]
        public Object GetBooks(string search, string paging)
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
            return new
            {
                ResultLines = JsonConvert.SerializeObject(books.Skip(pagingData.CurrentPageIndex * pagingData.PageSize).Take(pagingData.PageSize).ToList()),
                TotalLines = total
            };

        }

        public bool SaveBook(string book, string categories)
        {
            var categsToAdd = JsonConvert.DeserializeObject<List<BookCategory>>(categories);
            var bookData = JsonConvert.DeserializeObject<Book>(book);

            if (categsToAdd.Any())
            {
                foreach (var catToadd in categsToAdd)
                {
                    db.Categories.Add(new BookCategory
                    {
                        CategoryName = catToadd.CategoryName
                    });
                }
                db.SaveChanges();
            }

            var bookCategory = bookData.Category.CategoryId == -1
                ? db.Categories.SingleOrDefault(la => la.CategoryName == bookData.Category.CategoryName)
                : db.Categories.SingleOrDefault(la => la.CategoryId == bookData.Category.CategoryId);

            if (bookCategory == null)
            {
                return false;
            }

            if (bookData.BookId == 0)
            {
                var newBook = new Book
                {
                    Author = bookData.Author,
                    Name = bookData.Name,
                    Description = bookData.Description,
                    ISBN = bookData.ISBN,
                    Category = bookCategory,
                    Status = (int)BookStatus.ReadyForPickup
                };

                db.Books.Add(newBook);
            }
            else
            {
                var bookToEdit = db.Books.SingleOrDefault(b => b.BookId == bookData.BookId);
                if (bookToEdit != null)
                {
                    bookToEdit.Author = bookData.Author;
                    bookToEdit.Description = bookData.Description;
                    bookToEdit.ISBN = bookData.ISBN;
                    bookToEdit.Category = bookCategory;
                    bookToEdit.Name = bookData.Name;
                }
            }
            db.SaveChanges();
            return true;
        }

        public void DeleteBook(int id)
        {
            var bookToDelete = db.Books.SingleOrDefault(book => book.BookId == id);
            if (bookToDelete != null)
            {
                db.Books.Remove(bookToDelete);
            }
            db.SaveChanges();
        }

        public IEnumerable<BookCategory> GetBookCategories()
        {
            return db.Categories;
        }

        public IEnumerable<ReservedBook> GetUserBooks()
        {
            var res = db.LibraryUsers.SelectMany(
                    usr => usr.UserBookCollection.Where(la => DbFunctions.TruncateTime(la.StartDate) <= DateTime.Today
                                                              &&
                                                              (la.Book.Status == (int)BookStatus.Issued ||
                                                               la.Book.Status == (int)BookStatus.ReadyForPickup)))
                    .ToList().Select(la1 => new ReservedBook
                    {
                        ReserveId = la1.UserToBookId,
                        UserName = la1.LibraryUser.UserName,
                        BookId = la1.BookId,
                        EndDate = la1.EndDate.ToString("d MMM yyyy"),
                        StartDate = la1.StartDate.ToString("d MMM yyyy"),
                        Name = la1.Book.Name,
                        Status = la1.Status,
                        UserId = la1.LibraryUserId
                    });
            return res;
        }

        public void IssueTheBook(int bookId, int reserveId)
        {
            db.Books.Single(book => book.BookId == bookId).Status = (int)BookStatus.Issued;
            db.UsersToBooks.Single(la => la.UserToBookId == reserveId).Status = (int)ReserveStatus.WaitForReturn;
            db.SaveChanges();
        }

        public void ReturnTheBook(int bookId, int reserveId, int newSatus)
        {
            var retBook = db.Books.Single(book => book.BookId == bookId);
            retBook.Status = newSatus;
            db.UsersToBooks.Remove(db.UsersToBooks.Single(la => la.UserToBookId == reserveId));
            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}