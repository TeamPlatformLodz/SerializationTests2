using Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cokolwiek
{
    public interface IDataInserter
    {
        void InitializeContextWithData(ShopContext context);
    }

    public class ConstantDataInserter : IDataInserter
    {
        public void InitializeContextWithData(ShopContext context)
        {
            var names = new List<string>()
            {
                "Buddy",
                "Peter",
                "Tonny",
                "Janick",
                "Johnny"
            };
            var lastNames = new List<string>()
            {
                "Guy",
                "Green",
                "Iommi",
                "Gers",
                "Cash"
            };
            var products = new Product[5];
            var states = new ProductState[5];
            products[0] = new Product("Fender Stratocaster");
            states[0] = new ProductState(products[0], 5, (decimal)2200, new Percentage(23));
            products[1] = new Product("Fender Telecaster");
            states[1] = new ProductState(products[1], 2, (decimal)2101.12, new Percentage(23));
            products[2] = new Product("Jaydee SG");

            for (int i = 0; i < names.Count; i++)
            {
                context.Clients.Add(new Client(
                    names[i],
                    lastNames[i]
                    ));
            }
            for (int i = 0; i < 2; i++)
            {
                context.Products.Add(products[i].Id, products[i]);
                context.ProductStates.Add(states[i]);
            }
            var clients = context.Clients;
            context.Invoices.Add(new Invoice(
                clients[0],
                products[1],
                1,
                (decimal)123.123,
                new Percentage(21)
                ));

        }
    }
}
