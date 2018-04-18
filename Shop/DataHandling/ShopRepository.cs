using Shop.Exceptions;
using Shop.Logging;
using Shop.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop
{
    /// <summary>
    /// Repository manages data for shop apllication.
    /// Uses injected ILogger for logging data context changes.
    /// </summary>
    public class ShopRepository
    {
        private ShopContext context;
        private ILogger logger;

        public ShopRepository (ShopContext context, ILogger logger)
        {
            this.context = context;
            this.logger = logger;
            #region Add logging when collections are changed
            context.Invoices.CollectionChanged += CollectionChanged;
            context.ProductStates.CollectionChanged += CollectionChanged;
            #endregion
        }

        #region GetAll
        public ICollection<Client> GetAllClients() => context.Clients.ToList();
        public ICollection<Product> GetAllProducts() => context.Products.Values.ToList();
        public ICollection<ProductState> GetAllProductStates() => context.ProductStates.ToList();
        public ICollection<Invoice> GetAllInvoices() => context.Invoices.ToList();
        #endregion

        #region Get
        public Client GetClient(string id)
        {
            var client = context.Clients.Find(c => c.Id == id);
            if ( client is null)
            {
                throw new NotFoundException("Client not found");
            }
            else
            {
                return client;
            }
        }
        public Product GetProduct(string id)
        {
            try
            {
                var product = context.Products[id];
                return product;
            }
            catch(KeyNotFoundException)
            {
                throw new NotFoundException("Product not found");
            }
           
        }
        public Invoice GetInvoice(string id)
        {
            var invoice = context.Invoices.FirstOrDefault(i => i.Id == id);
            if ( invoice is null)
            {
                throw new NotFoundException("Invoice not found");
            }
            else
            {
                return invoice;
            }
        }
        public ProductState GetProductState(Product product) 
        {
            var productState = context.ProductStates.FirstOrDefault(p => p.Product.Id == product.Id);
            if (productState is null)
            {
                throw new NotFoundException("ProductState not found");
            }
            else
            {
                return productState;
            }
        }
        public ReportData GetReportData()
        {
            return context.ReportData;
        }
        #endregion

        #region Add
        public void Add(Client client)
        {
            if (client != null)
            {
                if (context.Clients.Find(c => c.Id == client.Id) == null) // if no Client with given id
                {
                    context.Clients.Add(client);
                    context.ReportData.LastChangeTime = DateTime.Now;
                }
                else
                {
                    throw new DuplicateException("Cannot add client with identical id.");
                }
            }
            else new ArgumentNullException("Client can not be null");
        }
        public void Add(Product product)
        {
            if (product != null)
            {
                if (!context.Products.ContainsKey(product.Id)) // if no Product with given id
                {
                    context.Products.Add(product.Id, product);
                    context.ReportData.LastChangeTime = DateTime.Now;
                }
                else
                {
                    throw new DuplicateException("Cannot add product with identical id.");
                }
            }
            else new ArgumentNullException("Product can not be null");
            
        }
        public void Add(Invoice invoice)
        {
            if (invoice != null)
            {
                if (invoice.Client != null)
                {
                    if (invoice.Product != null)
                    {
                        var isClientUnknown = !context.Clients.Contains(invoice.Client);
                        if (isClientUnknown)
                        {
                            try
                            {
                                context.Clients.Add(invoice.Client);
                            }
                            catch { throw; }
                        }

                        context.Invoices.Add(invoice);
                        context.ReportData.LastChangeTime = DateTime.Now;

                    }
                    else throw new ArgumentNullException("Product can not be null");
                }
                else throw new ArgumentNullException("Client can not be null");
            }
            else throw new ArgumentNullException("Invoice can not be null");
        }
        public void Add(ProductState productState)
        {
            if (productState != null)
            {
                if (productState.Product != null)
                {
                    if (context.ProductStates.FirstOrDefault(p => p.Product.Id == productState.Product.Id) == null) // if no ProductState describing the same product
                    {
                        context.ProductStates.Add(productState);
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                    else
                    {
                        throw new DuplicateException("Cannot add productState describing the same product.");
                    }
                }
                else throw new ArgumentNullException("Can not add ProductState without Product");
            }
            else throw new ArgumentNullException("Product can not be null");
        }
    
        #endregion

        #region Delete
        public void Delete(Client client)
        {
            if(client != null)
            {
                if (context.Invoices.FirstOrDefault(i => i.Client.Id == client.Id) == null)
                {
                    if (context.Clients.Contains(client))
                    {
                        context.Clients.Remove(client);
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                    else
                    {
                        throw new NotFoundException("Client not found.");
                    }
                }
                else throw new DeleteReferencesException("Can not delete Client because of references from Invoice(s)");                   
            }
            else throw new ArgumentNullException("Client can not be null");
        }
        public void Delete(Product product)
        {
            if (product != null)
            {
                if (context.Invoices.FirstOrDefault(i => i.Product.Id == product.Id) == null)
                {
                    if (context.Products.ContainsKey(product.Id))
                    {
                        context.Products.Remove(product.Id);
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                    else
                    {
                        throw new NotFoundException("Product not found.");
                    }
                }
                else throw new DeleteReferencesException("Can not delete Product because of references from Invoice(s)");
            }
            else throw new ArgumentNullException("Product can not be null");
            
        }
        public void Delete(Invoice invoice)
        {
            if (invoice != null)
            {
                    if (context.Invoices.Contains(invoice))
                    {
                        context.Invoices.Remove(invoice);
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                    else
                    {
                        throw new NotFoundException("Invoice not found");
                    }
               
            }
            else throw new ArgumentNullException("Invoice can not be null");
            
        }
        public void Delete(ProductState productState)
        {
            if (productState.Product != null)
            {
                if (context.ProductStates.Contains(productState))
                {
                    context.ProductStates.Remove(productState);
                    context.ReportData.LastChangeTime = DateTime.Now;
                    
                }
                else
                {
                    throw new NotFoundException("Product State not found");
                }
            }
            else throw new DeleteReferencesException("Can not delete ProductData because of the reference to some Product");

        }
        #endregion

        #region Update
        public void Update(Client client, ClientData clientData)
        {
            if (client != null )
            {
                if (clientData != null)
                {
                    if (clientData.IsFirstNameChanged == true)
                    {
                        client.FirstName = clientData.FirstName;
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                    if (clientData.IsLastNameChanged == true)
                    {
                        client.LastName = clientData.LastName;
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                }
                else throw new ArgumentNullException("ClientData can not be null");
            }
            else throw new ArgumentNullException("Client can not be null");
        }
        public void Update(Product product, ProductData productData)
        {
            if (product != null)
            {
                if (productData != null)
                {
                    if (productData.IsNameChanged == true)
                    {
                        product.Name = productData.Name;
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                }
                else throw new ArgumentNullException("ProductData can not be null");
            }
            else throw new ArgumentNullException("Product can not be null");
        }
        public void Update(ProductState productState, ProductStateData productStateData)
        {
            if (productState != null)
            {
                if (productStateData != null)
                {
                    if (productStateData.IsAmountChanged == true)
                    {
                        productState.Amount = productStateData.Amount;
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                    if (productStateData.IsPriceNettoChanged == true)
                    {
                        productState.PriceNetto = productStateData.PriceNetto;
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                    if (productStateData.IsTaxRateChanged == true)
                    {
                        productState.TaxRate = productStateData.TaxRate;
                        context.ReportData.LastChangeTime = DateTime.Now;
                    }
                }
                else throw new ArgumentNullException("ProductStateData can not be null");
            }
            else throw new ArgumentNullException("ProductState can not be null");
        }
        public void Update(Invoice invoice, InvoiceData invoiceData)
        {
            if (invoice != null)
            {
                if (invoice.Client != null)
                {
                    if (invoice.Product != null)
                    {
                        if (invoiceData != null)
                        {
                            if (invoiceData.IsPurchaseTimeChanged == true)
                            {
                                invoice.PurchaseTime = invoiceData.PurchaseTime;
                                context.ReportData.LastChangeTime = DateTime.Now;
                            }
                            if (invoiceData.IsAmountChanged == true)
                            {
                                invoice.Amount = invoiceData.Amount;
                                context.ReportData.LastChangeTime = DateTime.Now;
                            }
                            if (invoiceData.IsTaxRateChanged == true)
                            {
                                invoice.TaxRate = invoiceData.TaxRate;
                                context.ReportData.LastChangeTime = DateTime.Now;
                            }
                            if (invoiceData.IsPriceChanged == true)
                            {
                                invoice.Price = invoiceData.Price;
                                context.ReportData.LastChangeTime = DateTime.Now;
                            }
                        }
                        else throw new ArgumentNullException("InvoiceData can not be null");
                    }
                    else throw new ArgumentNullException("Product can not be null");
                }
                else throw new ArgumentNullException("Client can not be null");
            }
            else throw new ArgumentNullException("Invoice can not be null");
        }
        #endregion

        public void SetContext(ShopContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Creates message about changes and calls ILogger.Log() to log message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                StringBuilder itemsMessageBuilder = new StringBuilder();
                foreach(var item in e.NewItems)
                {
                    itemsMessageBuilder.Append(item.ToString() + " ");
                }
                logger.Log($"Added {itemsMessageBuilder} to {sender.ToString()}");
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                StringBuilder itemsMessageBuilder = new StringBuilder();
                foreach (var item in e.OldItems)
                {
                    itemsMessageBuilder.Append(item.ToString() + " ");
                }
                logger.Log($"Removed {itemsMessageBuilder} from {sender.ToString()}");
            }
        }
    }
}
