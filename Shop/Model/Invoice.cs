using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop
{
    [Serializable]
    public class Invoice
    {
        private string id;

        public string Id { get => id; }
        public DateTime PurchaseTime { get; set; }
        public Client Client { get; set; }
        public Product Product { get; set; }
        public int Amount { get; set; }
        public decimal Price { get; set; }
        public Percentage TaxRate { get; set; }

        public Invoice(Client client, Product product, int amount, decimal price, Percentage taxRate)
        {
            id = Guid.NewGuid().ToString();
            PurchaseTime = DateTime.Now;
            Product = product;
            Client = client;
            Amount = amount;
            Price = price;
            TaxRate = taxRate;
        }

        public override string ToString()
        {
            return $"--Date {PurchaseTime.ToString()} \n--Buyer: {Client.ToString()} \n--Product: {Product.ToString()}" +
                    $" \n--Amount: {Amount.ToString()} \n--Netto Price: {Price.ToString()} \n--Tax Rate: {TaxRate.ToString()} ";
        }

    }
}
