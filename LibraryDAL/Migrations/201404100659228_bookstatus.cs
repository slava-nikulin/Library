namespace LibraryDAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class bookstatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Books", "Status", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Books", "Status");
        }
    }
}
