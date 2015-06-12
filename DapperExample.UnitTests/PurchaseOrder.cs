namespace DapperExample.UnitTests
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string Item { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }

        public override bool Equals(object obj)
        {
            var purchaseOrder = obj as PurchaseOrder;
            if (purchaseOrder != null)
            {
                return (Id == purchaseOrder.Id) && (string.Equals(Item, purchaseOrder.Item))
                       && (string.Equals(Description, purchaseOrder.Description))
                       && Amount == purchaseOrder.Amount;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new {Id, Item, Description, Amount}.GetHashCode();
        }
    }
}