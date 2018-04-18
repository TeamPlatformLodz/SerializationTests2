using Shop;
using Shop.Logging;
using System;
using System.Collections.Generic;
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
            var list = new List<Car>();
            list.Add(car1);
            list.Add(car2);
            var driver = new Driver()
            {
                Car = list,
                Owner = owner
            };
            form.Serialize(fs, driver);
            FileStream deserFS = new FileStream("tekst.txt", FileMode.Open);
            //var deser = form.Deserialize(deserFS);
            var line = "{Type=System.String Name=LastName}=Iommi";
            var sth = RH.LookForMetadata(line);
            var res = RH.LookForValue(line);
            List<Client> cl = new List<Client>();
        }
        internal class Driver
        {
            public List<Car> Car { get; set; }
            public Owner Owner { get; set; }
        }
    }
}
