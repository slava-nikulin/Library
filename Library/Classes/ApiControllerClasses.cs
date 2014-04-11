using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using LibraryDAL;

namespace Library.Classes
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
        public string UserName { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public int BookId { get; set; }
        [DataMember]
        public int UserId { get; set; }
        [DataMember]
        public string EndDate { get; set; }
        [DataMember]
        public string StartDate { get; set; }
        [DataMember]
        public int Status { get; set; }
    }
}