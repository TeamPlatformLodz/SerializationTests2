using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cokolwiek
{
    class Program
    {
        static void Main(string[] args)
        {
            IFormatter form = new CustomFormatter();

            FileStream fs = new FileStream("tekst.txt", FileMode.Create);
            var owner = new Owner()
            {
                Name="Hanka"
            };
            var car1 = new Car()
            {
                Model="Punciak",
                Owner = owner
            };
            var car2 = new Car()
            {
                Model = "Punciak2",
                Owner = owner
            };
            List<Car> cars = new List<Car>() {car1, car2 };
            TestContext con = new TestContext()
            {
                MyCar = car1,
                MyOwner = owner
            };

            form.Serialize(fs, con);


            //FileStream deFS = new FileStream("tekst.txt", FileMode.Open);
            //var deCar = form.Deserialize(deFS);
            ////DataContext dcx = new DataContext(cars);

            var line = "@ID=123 ClassName=cokolwiek.Car";
            var delimiters = new char[] {' ', '=' };
            var id = line.Split(delimiters);
        }
    }

    internal class DataContext
    {
        public DataContext(List<Car> cars)
        {
           
        }
    }
    [Serializable]
    internal class TestContext
    {
        public Car MyCar { get; set; }
        public Owner MyOwner { get; set; }
    }
}
