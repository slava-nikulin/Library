namespace LibraryDAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rename1 : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.BookTags", newName: "BooksToTags");
        }
        
        public override void Down()
        {
            RenameTable(name: "dbo.BooksToTags", newName: "BookTags");
        }
    }
}
