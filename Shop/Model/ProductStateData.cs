using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Model
{
    public class ProductStateData
    {       
        private int amount;
        private decimal priceNetto;
        private Percentage taxRate;

        public int Amount { get => amount; set { amount = value; IsAmountChanged = true; } }
        public bool IsAmountChanged { get; set; } = false;
        public decimal PriceNetto { get => priceNetto; set { priceNetto = value; IsPriceNettoChanged = true; } }
        public bool IsPriceNettoChanged { get; set; } = false;
        public Percentage TaxRate { get => taxRate; set { taxRate = value; IsTaxRateChanged = true; } }
        public bool IsTaxRateChanged { get; set; } = false;
    }
}
