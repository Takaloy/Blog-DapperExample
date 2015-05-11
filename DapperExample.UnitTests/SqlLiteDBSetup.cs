using NUnit.Framework;
using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;
using System.Threading.Tasks;

namespace DapperExample.UnitTests
{
    [TestFixture]
    public class SqlLiteDBSetup
    {
        [SetUp]
        public void Setup()
        {
            DatabaseRepository.CreateDatabaseRepository();
        }

        [Test]
        public void SimpleDataAccessTest()
        {
            var database = new DatabaseRepository();
            var customerToInsert = new Customer
            {
                Id = 5,
                FirstName = "John",
                LastName = "Doe"
            };

            using (var connection = DatabaseRepository.SimpleDbConnection())
            {
                connection.Open();
                CreateTable(connection);

                InsertCustomer(connection, customerToInsert);
            }            

            using (var newconnection = DatabaseRepository.SimpleDbConnection())
            {
                var customerRetrieved = RetrieveCustomer(newconnection, customerToInsert.Id);

                Assert.AreEqual(customerToInsert.FirstName, customerRetrieved.FirstName);
                Assert.AreEqual(customerToInsert.LastName, customerRetrieved.LastName);
            }
        }

        private void CreateTable(IDbConnection connection)
        {
            connection.Execute(
                @"create table CUS_CUSTOMERS (
                    Id integer primary key , 
                    FirstName varchar(100) not null, 
                    LastName varchar(100) not null)"
                );            
        }

        private void InsertCustomer(IDbConnection connection, Customer customer)
        {        
            connection.Execute("insert into CUS_CUSTOMERS (ID, FirstName, LastName) values (@Id, @FirstName, @LastName);", customer);
        }

        private Customer RetrieveCustomer(IDbConnection connection, int id)
        {
            var result = connection.Query<Customer>(@"
                select Id, FirstName, LastName from CUS_CUSTOMERS where Id = @Id
                    ", new { Id = id }).FirstOrDefault();

            return result;
        }

        [TearDown]
        public void Tear()
        {
            DatabaseRepository.TearDownDatabaseRepository();
        }
    }
}
