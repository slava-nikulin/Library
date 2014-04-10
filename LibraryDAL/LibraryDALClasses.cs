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

        [DataMember]
        public int Status { get; set; }

        [ScriptIgnore]
        public virtual ICollection<UserBook> UserBookCollection { get; set; }
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


    public enum BookStatus
    {
        ReadyForPickup = 1,
        Issued = 2,
        Lost = 3
    }
}
