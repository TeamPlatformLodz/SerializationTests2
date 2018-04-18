using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Model
{
    public class InvoiceData
    {
        private DateTime purchaseTime;
        private int amount;
        private decimal price;
        private Percentage taxRate;

        public DateTime PurchaseTime {get => purchaseTime; set { purchaseTime = value; IsPurchaseTimeChanged = true;} }
        public bool IsPurchaseTimeChanged { get; set; } = false;
        public int Amount { get => amount; set { amount = value; IsAmountChanged = true; } }
        public bool IsAmountChanged { get; set; } = false;
        public decimal Price { get => price; set { price = value; IsPriceChanged = true; } }
        public bool IsPriceChanged { get; set; } = false;
        public Percentage TaxRate { get => taxRate; set { taxRate = value; IsTaxRateChanged = true; } }
        public bool IsTaxRateChanged { get; set; } = false;
    }
}
