namespace LibraryDAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class tags_category_for_book2 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BookCategories",
                c => new
                    {
                        CategoryId = c.Int(nullable: false, identity: true),
                        CategoryName = c.String(),
                    })
                .PrimaryKey(t => t.CategoryId);
            
            CreateTable(
                "dbo.BookTags",
                c => new
                    {
                        TagId = c.Int(nullable: false, identity: true),
                        TagName = c.String(),
                    })
                .PrimaryKey(t => t.TagId);
            
            CreateTable(
                "dbo.BookTag",
                c => new
                    {
                        BookId = c.Int(nullable: false),
                        TagId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.BookId, t.TagId })
                .ForeignKey("dbo.Books", t => t.BookId, cascadeDelete: true)
                .ForeignKey("dbo.BookTags", t => t.TagId, cascadeDelete: true)
                .Index(t => t.BookId)
                .Index(t => t.TagId);
            
            AddColumn("dbo.Books", "CategoryId", c => c.Int(nullable: false));
            CreateIndex("dbo.Books", "CategoryId");
            AddForeignKey("dbo.Books", "CategoryId", "dbo.BookCategories", "CategoryId", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.BookTag", "TagId", "dbo.BookTags");
            DropForeignKey("dbo.BookTag", "BookId", "dbo.Books");
            DropForeignKey("dbo.Books", "CategoryId", "dbo.BookCategories");
            DropIndex("dbo.BookTag", new[] { "TagId" });
            DropIndex("dbo.BookTag", new[] { "BookId" });
            DropIndex("dbo.Books", new[] { "CategoryId" });
            DropColumn("dbo.Books", "CategoryId");
            DropTable("dbo.BookTag");
            DropTable("dbo.BookTags");
            DropTable("dbo.BookCategories");
        }
    }
}
