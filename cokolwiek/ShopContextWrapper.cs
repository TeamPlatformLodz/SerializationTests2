using Shop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cokolwiek
{
    class ShopContextWrapper
    {
        public List<Client> Clients { get; set; } = new List<Client>();
        public List<string> ProductKeys { get; set; }
        public List<Product> Products { get; set; }
        public List<Invoice> Invoices { get; set; } = new List<Invoice>();
        public List<ProductState> ProductStates { get; set; } = new List<ProductState>();

        public ReportData ReportData { get; set; } = new ReportData();

        public ShopContextWrapper() { }

        public ShopContextWrapper(ShopContext context)
        {
            this.Clients = context.Clients;
            Invoices = context.Invoices.ToList();
            ProductStates = context.ProductStates.ToList();
            Products = new List<Product>();
            Products = context.Products.Values.ToList();
            ProductKeys = new List<string>();
            ProductKeys = context.Products.Keys.ToList();
        }

        public ShopContext GetContext()
        {
            ShopContext context = new ShopContext()
            {
                Clients = this.Clients,
            };
            //prod
            foreach (var prod in Products)
            {
                context.Products.Add(prod.Id, prod);
            }
            //
            context.Invoices = new ObservableCollection<Invoice>(Invoices);
            context.ProductStates = new ObservableCollection<ProductState>(ProductStates);
            return context;
        }
    }


}
