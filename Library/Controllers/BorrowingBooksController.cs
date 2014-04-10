﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.Http;
using Library.Models;
using LibraryDAL;
using Library.Classes;
using Newtonsoft.Json;

namespace Library.Controllers
{
    [System.Web.Http.Authorize]
    public class BorrowingBooksController : ApiController
    {
        [Serializable]
        [DataContract]
        public class ListResult
        {
            [DataMember]
            public int TotalLines { get; set; }
            [DataMember]
            public IEnumerable<Book> ResultLines { get; set; }
        }

        [Serializable]
        [DataContract]
        public class PagingData
        {
            [DataMember(Name = "CurrentPageIndex")]
            public int CurrentPageIndex { get; set; }
            [DataMember(Name = "PageSize")]
            public int PageSize { get; set; }
            [DataMember(Name = "CurrentColumn")]
            public string CurrentColumn { get; set; }
            [DataMember(Name = "SortType")]
            public string SortType { get; set; }
        }

        [Serializable]
        [DataContract]
        public class ReservedBook
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Author { get; set; }
            [DataMember]
            public int BookId { get; set; }
            [DataMember]
            public string EndDate { get; set; }
            [DataMember]
            public string StartDate { get; set; }
            [DataMember]
            public int Status { get; set; }
        }


        private LibraryContext db = new LibraryContext();

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

        public IEnumerable<string> GetBookBorrowingData(int bookId)
        {
            var currentBookDates = db.Books.Single(book => book.BookId == bookId).UserBookCollection;
            var dates = new List<DateTime>();

            foreach (var reserve in currentBookDates)
            {
                for (var dt = reserve.StartDate; dt <= reserve.EndDate; dt = dt.AddDays(1))
                {
                    if (dates.All(la => !la.Equals(dt)))
                    {
                        dates.Add(dt);
                    }
                }
            }

            return dates.Select(la => la.ToString("dd-MM-yyyy"));
        }

        public bool ReserveTheBook(AccountRoomModel model)
        {
            var selectedBook = db.Books.SingleOrDefault(book => book.BookId == model.BorrowLend.BookId);

            DateTime fromDateTime;
            DateTime toDateTime;

            if (!DateTime.TryParseExact(model.BorrowLend.FromDate, "dd-MM-yyyy", null, DateTimeStyles.None, out fromDateTime))
            {
                if (!DateTime.TryParseExact(model.BorrowLend.FromDate, "dd/MM/yyyy", null, DateTimeStyles.None, out fromDateTime))
                {
                    if (!DateTime.TryParseExact(model.BorrowLend.FromDate, "dd.MM.yyyy", null, DateTimeStyles.None, out fromDateTime))
                    {
                        return false;
                    }
                }
            }
            if (!DateTime.TryParseExact(model.BorrowLend.ToDate, "dd-MM-yyyy", null, DateTimeStyles.None, out toDateTime))
            {
                if (!DateTime.TryParseExact(model.BorrowLend.ToDate, "dd/MM/yyyy", null, DateTimeStyles.None, out toDateTime))
                {
                    if (!DateTime.TryParseExact(model.BorrowLend.ToDate, "dd.MM.yyyy", null, DateTimeStyles.None, out toDateTime))
                    {
                        return false;
                    }
                }
            }

            if (toDateTime < fromDateTime)
            {
                return false;
            }

            if (selectedBook != null)
            {
                selectedBook.UserBookCollection.Add(new UserBook
                {
                    Book = selectedBook,
                    BookId = selectedBook.BookId,
                    EndDate = toDateTime,
                    LibraryUser = db.LibraryUsers.Single(usr => usr.UserName == User.Identity.Name),
                    LibraryUserId = db.LibraryUsers.Single(usr => usr.UserName == User.Identity.Name).LibraryUserId,
                    StartDate = fromDateTime
                });
            }
            db.SaveChanges();

            return true;
        }

        public IEnumerable<ReservedBook> GetUserBooks()
        {
            return db.LibraryUsers.Single(usr => usr.UserName == User.Identity.Name).UserBookCollection.Select(la => new ReservedBook
            {
                BookId = la.BookId,
                Author = la.Book.Author,
                Name = la.Book.Name,
                EndDate = la.EndDate.ToString("d MMM yyyy"),
                StartDate = la.StartDate.ToString("d MMM yyyy"),
                Status = la.Book.Status
            });
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}