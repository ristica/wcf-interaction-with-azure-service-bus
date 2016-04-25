using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel;
using Core.Common.Contracts;
using Core.Common.Exceptions;
using Demo.Business.Common;
using Demo.Business.Contracts;
using Demo.Business.Entities;
using Demo.Data.Contracts;

namespace Demo.Business.Managers
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerCall, 
        ConcurrencyMode = ConcurrencyMode.Multiple, 
        ReleaseServiceInstanceOnTransactionComplete = false)]
    public class InventoryManager : ManagerBase, IInventoryService
    {
        #region Fields

        [Import]
        private IDataRepositoryFactory _repositoryFactory;

        [Import]
        private IBusinessEngineFactory _businessFactory;

        #endregion

        #region C-Tor

        /// <summary>
        /// default c-tor for wcf
        /// </summary>
        public InventoryManager()
        {

        }

        /// <summary>
        /// for test purposes
        /// </summary>
        /// <param name="repositoryFactory"></param>
        public InventoryManager(IDataRepositoryFactory repositoryFactory)
        {
            this._repositoryFactory = repositoryFactory;
        }

        /// <summary>
        /// for test purposes
        /// </summary>
        /// <param name="businessFactory"></param>
        public InventoryManager(IBusinessEngineFactory businessFactory)
        {
            this._businessFactory = businessFactory;
        }

        /// <summary>
        /// for test purposes
        /// </summary>
        /// <param name="repositoryFactory"></param>
        /// <param name="businessFactory"></param>
        public InventoryManager(IDataRepositoryFactory repositoryFactory, IBusinessEngineFactory businessFactory)
        {
            this._repositoryFactory = repositoryFactory;
            this._businessFactory = businessFactory;
        }

        #endregion

        #region IInventoryManager implementation

        public Product[] GetProducts()
        {
            return ExecuteFaultHandledOperation(() =>
            {
                // just for debug purposes!!!!
                var ctx = ServiceSecurityContext.Current;

                var productRepository = this._repositoryFactory.GetDataRepository<IProductRepository>();
                var products = productRepository.GetProducts();

                return products.ToArray();
            });
        }

        public Product[] GetActiveProducts()
        {
            return ExecuteFaultHandledOperation(() =>
            {
                var productRepository = this._repositoryFactory.GetDataRepository<IProductRepository>();
                var products = productRepository.GetActiveProducts();

                return products;
            });
        }

        public Product GetProductById(int id, bool acceptNullable = false)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                var productRepository = this._repositoryFactory.GetDataRepository<IProductRepository>();
                var product = productRepository.GetProductById(id);

                if (product == null && acceptNullable)
                {
                    return null;
                }

                if (product == null)
                {
                    var ex = new NotFoundException($"Product with id: {id} not found!");
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                return product;
            });
        }

        [TransactionFlow(TransactionFlowOption.Allowed)]
        [OperationBehavior(TransactionScopeRequired = true)]
        public Product UpdateProduct(Product product)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                var productRepository = this._repositoryFactory.GetDataRepository<IProductRepository>();
                if (product.ProductId == 0)
                {
                    product.ArticleNumber =
                        this._businessFactory.GetBusinessEngine<IProductInventoryEngine>().GenerateArticleNumber();
                }

                var updatedEntity = productRepository.UpdateProduct(product);
                return updatedEntity;
            });
        }

        [TransactionFlow(TransactionFlowOption.Allowed)]
        [OperationBehavior(TransactionScopeRequired = true)]
        public void DeleteProduct(int productId)
        {
            ExecuteFaultHandledOperation(() =>
            {
                var productRepository = this._repositoryFactory.GetDataRepository<IProductRepository>();
                var product = productRepository.GetProductById(productId);

                if (product == null)
                {
                    var ex = new NotFoundException($"Product with id: {productId} not found!");
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                product.IsActive = false;
                var result = productRepository.UpdateProduct(product);
            });
        }

        [TransactionFlow(TransactionFlowOption.Allowed)]
        [OperationBehavior(TransactionScopeRequired = true)]
        public void ActivateProduct(int productId)
        {
            ExecuteFaultHandledOperation(() =>
            {
                var productRepository = this._repositoryFactory.GetDataRepository<IProductRepository>();
                var product = productRepository.GetProductById(productId);

                if (product == null)
                {
                    var ex = new NotFoundException($"Product with id: {productId} not found!");
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                product.IsActive = true;
                var result = productRepository.UpdateProduct(product);
            });
        }

        #endregion        
    }
}
