using Shop;
using Shop.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cokolwiek
{
    public class Program
    {
        static void Main(string[] args)
        {
            IFormatter form = new CustomFormatter();

            FileStream fs = new FileStream("tekst.txt", FileMode.Create);

            var logger = new FileLogger("FileLogger.txt");
            var context = new ShopContext();
            var inserter = new ConstantDataInserter();
            inserter.InitializeContextWithData(context);
            var repo = new ShopRepository(context, logger);
            var service = new ShopService(repo, logger);

            var owner = new Owner()
            {
                Name = "TurTur"
            };
            var car1 = new Car()
            {
                Owner = owner,
                Model = "Niezly",
                Year = 1234
            };
            var car2 = new Car()
            {
                Owner = owner,
                Model = "gorszy",
                Year = 0022
            };
            var list = new List<Car>
            {
                car1,
                car2
            };
            var driver = new Driver()
            {
                Cars = new Car[] {car1, car2}
            };

            var prod = new ProductState(new Product("213"), 12, (decimal)14.32, new Percentage(12));

            Produktieren products = new Produktieren();
            products.states.Add(prod);

            ShopContextWrapper wrapper = new ShopContextWrapper(context);
            form.Serialize(fs, wrapper);

            FileStream deserFS = new FileStream("tekst.txt", FileMode.Open);
            var deser = form.Deserialize(deserFS);
            var deserContext = ((ShopContextWrapper)deser).GetContext();
            var client = deserContext.Invoices.First().Client;
            foreach (var cl in deserContext.Clients)
            {
                if(client == cl)
                {
                    cl.FirstName = "fafafaf";
                    Console.WriteLine("Szach mat!!!");
                }
            }
        }
        public class Produktieren
        {
            public ObservableCollection<ProductState> states { get; set; } = new ObservableCollection<ProductState>();
        }
        internal class Driver
        {
            public Car[] Cars { get; set; }
        }
    }
}
