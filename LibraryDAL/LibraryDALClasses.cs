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
                .HasForeignKey(c => c.LibraryUserId)
                .WillCascadeOnDelete(true);

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

        [ScriptIgnore]
        public virtual ICollection<UserBook> UserBookCollection { get; set; }
    }

    [Table("LibraryUsers")]
    public class LibraryUser
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int LibraryUserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public virtual ICollection<UserBook> UserBookCollection { get; set; }
    }

    [Table("UserBook")]
    public class UserBook
    {
        public int BookId { get; set; }
        public int LibraryUserId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual Book Book { get; set; }
        public virtual LibraryUser LibraryUser { get; set; }

        
    }
}
