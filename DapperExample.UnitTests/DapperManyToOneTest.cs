using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

        [Test]
        public void CoolWayIsFasterThanSimplistic()
        {
            #region setup
            var firstCustomer = new Customer
            {
                Id = 0,
                FirstName = "John",
                LastName = "Doe"
            };

            var secondCustomer = new Customer
            {
                Id = 1,
                FirstName = "Doctor",
                LastName = "Who"
            };

            var customerList = new List<Customer> {firstCustomer, secondCustomer};
            for (int i = 2; i < 10; i++)
            {
                customerList.Add(new Customer
                {
                    Id = i,
                    FirstName = (new Random(i).Next()).ToString(),
                    LastName = (new Random(i^2).Next()).ToString()
                });
            }

            using (var connection = DatabaseRepository.SimpleDbConnection())
            {
                connection.Open();
                CreateTable(connection);
                InsertCustomer(connection, customerList);
                GenerateRandomProducts(connection, customerList, 15);
            }
            #endregion

            #region proof they are equal
            using (var connection = DatabaseRepository.SimpleDbConnection())
            {
                connection.Open();
                var coolCustomers = GetCustomers(connection).First(s => s.Id == firstCustomer.Id);;
                var uncoolCustomers = GetCustomersTheUnCoolWay(connection).First(s => s.Id == firstCustomer.Id);;

                CollectionAssert.AreEquivalent(coolCustomers.PurchaseOrders, uncoolCustomers.PurchaseOrders);   //assert they are the same
            }
            #endregion

            #region compare speed

            var coolCustomerWatch = new Stopwatch();
            coolCustomerWatch.Start();
            for (var i = 0; i < 2000; i++)
            {
                using (var connection = DatabaseRepository.SimpleDbConnection())
                {
                    connection.Open();
                    GetCustomers(connection);
                }
            }
            coolCustomerWatch.Stop();
            var coolCustomerTimeOutput = coolCustomerWatch.ElapsedMilliseconds;
            Console.WriteLine("cool customers time : " + coolCustomerTimeOutput);

            var uncoolCustomerWatch = new Stopwatch();
            uncoolCustomerWatch.Start();
            for (var i = 0; i < 2000; i++)
            {
                using (var connection = DatabaseRepository.SimpleDbConnection())
                {
                    connection.Open();
                    GetCustomersTheUnCoolWay(connection);
                }
            }
            uncoolCustomerWatch.Stop();
            var uncoolCustomerTimeOutput = uncoolCustomerWatch.ElapsedMilliseconds;
            Console.WriteLine("uncool customers time : " + uncoolCustomerTimeOutput);
            #endregion
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

        private IList<Customer> GetCustomersTheUnCoolWay(IDbConnection connection)
        {
            var customers = connection.Query<Customer>(@"
                select
                    Id, FirstName, LastName
                from CUS_CUSTOMERS                
                ").ToList();

            foreach (var customer in customers)
            {
                var products = connection.Query<PurchaseOrder>(@"
                select 
                    p.Id, p.Item, p.Description, p.Amount
                from PO_PURCHASE_ORDERS as p
                where p.CusId = @Id
                ", new {customer.Id}).ToList();

                customer.PurchaseOrders = products;
            }

            return customers;
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