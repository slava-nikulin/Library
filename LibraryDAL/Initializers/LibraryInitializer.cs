using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryDAL.Initializers
{

    public class LibraryInitializer : IDatabaseInitializer<LibraryContext>

    {
        public void InitializeDatabase(LibraryContext context)
        {
            context.Database.CreateIfNotExists();
        }
    }
}
