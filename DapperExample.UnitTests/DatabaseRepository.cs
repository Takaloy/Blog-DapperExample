using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperExample.UnitTests
{
    public class DatabaseRepository
    {
        public static void CreateDatabaseRepository()
        {
            SQLiteConnection.CreateFile(DbFile);
        }

        public static void TearDownDatabaseRepository()
        {
            File.Delete(DatabaseRepository.DbFile);
        }

        public static string DbFile
        {
            get { return Environment.CurrentDirectory + "\\SimpleDb.sqlite"; }
        }

        public static SQLiteConnection SimpleDbConnection()
        {
            return new SQLiteConnection("Data Source=" + DbFile);
        }
    }
}
