using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Shop
{

    /// <summary>
    /// Represents data context for application.
    /// Consists of collections: clients, products, products states, invoices.
    /// Also holds report data for last generated context report.
    /// </summary>
    [Serializable]
    public class ShopContext : ISerializable
    {
        public ShopContext()
        {

        }
        
        protected ShopContext(SerializationInfo info, StreamingContext context)
        {
            info.GetValue("ShopContext_clients", Clients.GetType());
            info.AddValue("ShopContext_products", Products.GetType());
            info.AddValue("ShopContext_invoices", Invoices.GetType());
            info.AddValue("ShopContext_productStates", ProductStates.GetType());
        }

        public List<Client> Clients { get; set; } = new List<Client>();
        public Dictionary<string, Product> Products { get; set; } = new Dictionary<string, Product>();
        public ObservableCollection<Invoice> Invoices { get; set; } = new ObservableCollection<Invoice>();
        public ObservableCollection<ProductState> ProductStates { get; set; } = new ObservableCollection<ProductState>();

        public ReportData ReportData { get; set; } = new ReportData();

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ShopContext_clients", Clients, Clients.GetType());
            info.AddValue("ShopContext_products", Products, Products.GetType());
            info.AddValue("ShopContext_invoices", Invoices, Invoices.GetType());
            info.AddValue("ShopContext_productStates", ProductStates, ProductStates.GetType());
        }
    }
}
