using System;
using System.Data.SQLite;
using System.IO;

namespace DapperExample.UnitTests
{
    public class DatabaseRepository
    {
        public static string DbFile
        {
            get { return Environment.CurrentDirectory + "\\SimpleDb.sqlite"; }
        }

        public static void CreateDatabaseRepository()
        {
            SQLiteConnection.CreateFile(DbFile);
        }

        public static void TearDownDatabaseRepository()
        {
            File.Delete(DbFile);
        }

        public static SQLiteConnection SimpleDbConnection()
        {
            return new SQLiteConnection("Data Source=" + DbFile);
        }
    }
}