using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LibraryDAL
{
    public class LibraryContext : DbContext
    {
        public LibraryContext()
            : base("Library_dbConnection")
        {
            
        }

        public DbSet<LibraryUser> LibraryUsers { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BookCategory> Categories { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserBook>()
                .HasKey(c => new { c.BookId, c.LibraryUserId });

            modelBuilder.Entity<Book>()
                .HasMany(c => c.UserBookCollection)
                .WithRequired()
                .HasForeignKey(c => c.BookId);

            modelBuilder.Entity<LibraryUser>()
                .HasMany(c => c.UserBookCollection)
                .WithRequired()
                .HasForeignKey(c => c.LibraryUserId);

            modelBuilder.Entity<Book>()
                .HasRequired(book => book.Category)
                .WithMany(cat => cat.BooksInCategory)
                .HasForeignKey(book => book.CategoryId);
    
            modelBuilder.Entity<Book>()
                .HasMany(p => p.TagsCollection)
                .WithMany(t => t.BooksCollection)
                .Map(mc =>
                {
                    mc.ToTable("BookTag");
                    mc.MapLeftKey("BookId");
                    mc.MapRightKey("TagId");
                });


            Database.SetInitializer(new CreateDatabaseIfNotExists<LibraryContext>());
        }
    }

    [Serializable]
    [Table("Books")]
    //[KnownType(typeof(ICollection<UserBook>))]
    [DataContract]
    public class Book
    {
        [DataMember]
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int BookId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string ISBN { get; set; }

        [DataMember]
        public int Status { get; set; }


        [ScriptIgnore]
        public virtual ICollection<UserBook> UserBookCollection { get; set; }
        [ScriptIgnore]
        public int CategoryId { get; set; }
        [DataMember]
        public virtual BookCategory Category { get; set; }

        [DataMember]
        public virtual ICollection<BookTag> TagsCollection { get; set; }
    }

    [Table("LibraryUsers")]
    [DataContract]
    [Serializable]
    public class LibraryUser
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [DataMember]
        public int LibraryUserId { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public virtual ICollection<UserBook> UserBookCollection { get; set; }

        
    }

    [Table("UserBook")]
    [Serializable]
    [DataContract]
    public class UserBook
    {
        [DataMember]
        public int BookId { get; set; }
        [DataMember]
        public int LibraryUserId { get; set; }
        [DataMember]
        public DateTime StartDate { get; set; }
        [DataMember]
        public DateTime EndDate { get; set; }


        [ScriptIgnore]
        public virtual Book Book { get; set; }
        [ScriptIgnore]
        public virtual LibraryUser LibraryUser { get; set; }
    }

    [Table("BookCategories")]
    [Serializable]
    [DataContract]
    public class BookCategory
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [DataMember]
        public int CategoryId { get; set; }
        [DataMember]
        public string CategoryName { get; set; }
        [ScriptIgnore]
        public virtual ICollection<Book> BooksInCategory { get; set; }
    }

    [Table("BooksToTags")]
    [Serializable]
    [DataContract]
    public class BookTag
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int TagId { get; set; }
        [DataMember]
        public string TagName { get; set; }
        [ScriptIgnore]
        public virtual ICollection<Book> BooksCollection { get; set; }
    }


    public enum BookStatus
    {
        ReadyForPickup = 1,
        Issued = 2,
        Lost = 3
    }
}
