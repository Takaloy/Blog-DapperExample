using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using NUnit.Framework;

namespace DapperExample.UnitTests
{
    [TestFixture]
    public class DapperManyToOneTest
    {

        [SetUp]
        public void Setup()
        {
            DatabaseRepository.CreateDatabaseRepository();
        }

        [Test]
        public void SimpleDataAccessTest()
        {
            var firstCustomer = new Customer
            {
                Id = 5,
                FirstName = "John",
                LastName = "Doe"
            };

            var secondCustomer = new Customer
            {
                Id = 1,
                FirstName = "Doctor",
                LastName = "Who"
            };

            using (var connection = DatabaseRepository.SimpleDbConnection())
            {
                connection.Open();
                CreateTable(connection);
                var customers = new List<Customer> {firstCustomer, secondCustomer};
                InsertCustomer(connection, customers);
                GenerateRandomProducts(connection, customers, 5);
            }

            using (var newconnection = DatabaseRepository.SimpleDbConnection())
            {
                newconnection.Open();
                var customers = GetCustomers(newconnection);
                var customerRetrieved = customers.First(s => s.Id == firstCustomer.Id);

                Assert.AreEqual(firstCustomer.FirstName, customerRetrieved.FirstName);
                Assert.AreEqual(firstCustomer.LastName, customerRetrieved.LastName);
                Assert.AreEqual(5, customerRetrieved.PurchaseOrders.Count);
            }
        }

        private void InsertCustomer(IDbConnection connection, IList<Customer> customers)
        {
            connection.Execute("insert into CUS_CUSTOMERS (ID, FirstName, LastName) values (@Id, @FirstName, @LastName);", customers);
        }

        private void GenerateRandomProducts(IDbConnection connection, IList<Customer> customers, int productsToGenerate = 2)
        {
            var random = new Random();

            foreach (var customer in customers)
            {
                for (var i = 0; i < productsToGenerate; i++)
                {
                    connection.Execute(@"
                        insert into PO_PURCHASE_ORDERS (CusId, Amount, Description, Item)
                        values (@CusId, @Amount, @Description, @Item)
                        ", new
                    {
                        CusId = customer.Id,
                        Amount = random.Next(),
                        Description = string.Format("Random Product for Customer {0} number {1}", customer.LastName, i),
                        Item = string.Format("Item{0}", i)
                    });
                }
            }
        }

        private IList<Customer> GetCustomers(IDbConnection connection)
        {
            var lookup = new Dictionary<int, Customer>();

            var result = connection.Query<Customer, PurchaseOrder, Customer>(@"
                select 
                    c.Id, c.FirstName, c.LastName, p.CusId,
                    p.Id, p.Item, p.Description, p.Amount
                from CUS_CUSTOMERS as c  
                left join PO_PURCHASE_ORDERS as p on c.Id = p.CusId", (c, p) =>
            {
                Customer customer;

                if (!lookup.TryGetValue(c.Id, out customer))
                    lookup.Add(c.Id, customer = c);
                
                if (customer.PurchaseOrders == null)
                    customer.PurchaseOrders = new List<PurchaseOrder>();

                customer.PurchaseOrders.Add(p);

                return customer;
            }, splitOn: "CusId");

            return result.Distinct().ToList();
        }

        private void CreateTable(IDbConnection connection)
        {
            connection.Execute(
                @"  create table CUS_CUSTOMERS (
                        Id integer primary key , 
                        FirstName varchar(100) not null, 
                        LastName varchar(100) not null);

                    create table PO_PURCHASE_ORDERS (
                        Id integer primary key autoincrement,
                        CusId integer not null,
                        Item varchar(100) not null,
                        Description varchar(100),
                        Amount decimal null);
                   "
                );
        }

        [TearDown]
        public void Tear()
        {
            DatabaseRepository.TearDownDatabaseRepository();
        }
    }
}