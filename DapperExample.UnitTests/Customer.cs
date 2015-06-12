using System.Collections.Generic;

namespace DapperExample.UnitTests
{
    public class Customer
    {
        public Customer()
        {
            PurchaseOrders = new List<PurchaseOrder>();
        }

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public IList<PurchaseOrder> PurchaseOrders { get; internal set; }
    }
}