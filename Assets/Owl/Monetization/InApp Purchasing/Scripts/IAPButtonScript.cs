using UnityEngine;
#if USE_IAP
using UnityEngine.Purchasing;
#endif


namespace Owl.Monetization{
    [DefaultExecutionOrder(10)]
    public class IAPButtonScript : MonoBehaviour
    {
#if USE_IAP

        public IAPType iAPType =  new IAPType("removeAds", ProductType.NonConsumable) ;

        private bool IsPurchased
        {
            get => PlayerPrefs.GetInt("IAPPurchased" + iAPType.productID , 0) == 1 ? true : false;
            set => PlayerPrefs.SetInt("IAPPurchased" + iAPType.productID, value ? 1 : 0);
        }
        [Space(20)]
        public UnityEngine.UI.Text title;
        public UnityEngine.UI.Text description;
        public UnityEngine.UI.Text priceText;


        [Space(20)]
        public UnityEngine.Events.UnityEvent onPurchaseCompleted;
        public UnityEngine.Events.UnityEvent onPurchaseFailed;


        private void Start()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnPurchaseClick);
        }


        private void OnEnable()
        {
            IAPManager.OnIAPInitialized += OnIAPInitialize;
            IAPManager.OnIAPProcessPurchase += OnIAPPurchaseComplete;
            IAPManager.OnIAPPurchaseFailed += OnIAPPurchaseFailed;
            IAPManager.OnIAPEntitlementRestored += OnIAPEntitlementRestored;
            Initialize();

        }

        private void OnDisable()
        {
            IAPManager.OnIAPInitialized -= OnIAPInitialize;
            IAPManager.OnIAPProcessPurchase -= OnIAPPurchaseComplete;
            IAPManager.OnIAPPurchaseFailed -= OnIAPPurchaseFailed;
            IAPManager.OnIAPEntitlementRestored -= OnIAPEntitlementRestored;
        }

        private void Initialize()
        {
            CheckBought();

            if (!IAPManager.IsInitialized)
                return;

            var storeController = IAPManager.GetStoreController();

            if (storeController == null)
                return;

            var product = storeController.GetProductById(iAPType.productID);

            if (product?.metadata == null)
                return;

            if (title)
                title.text = product.metadata.localizedTitle;

            if (description)
                description.text = product.metadata.localizedDescription;

            if (priceText)
                priceText.text = product.metadata.localizedPriceString;
        }



        private void OnIAPInitialize(bool status)
        {
            Initialize();
        }

        private void OnIAPPurchaseComplete(PendingOrder order)
        {
            if (order == null)
                return;

            foreach (var item in order.CartOrdered.Items())
            {
                var product = item.Product;

                if (product == null)
                    continue;

                if (iAPType.productID != product.definition.id)
                    continue;

                onPurchaseCompleted?.Invoke();

                if (iAPType.productType == ProductType.NonConsumable)
                {
                    SetBought();
                }

                return;
            }
        }



        private void OnIAPPurchaseFailed(FailedOrder failedOrder)
        {
            if (failedOrder == null)
                return;

            foreach (var item in failedOrder.CartOrdered.Items())
            {
                var product = item.Product;

                if (product == null)
                    continue;

                if (iAPType.productID != product.definition.id)
                    continue;

                onPurchaseFailed?.Invoke();

                return;
            }
        }

        private void OnIAPEntitlementRestored(Product product)
        {
            if (product == null)
                return;

            if (product.definition.id != iAPType.productID)
                return;

            if (iAPType.productType != ProductType.NonConsumable)
                return;

            CustomDebug.Log(
                $"IAP Restored: {product.definition.id}"
            );

            SetBought();
        }





        private void CheckBought()
        {
            if (IsPurchased)
            {
                gameObject.SetActive(false);
            }
        }



        private void SetBought()
        {
            IsPurchased = true;

            if (iAPType.dependentProductIds != null)
            {
                foreach (var id in iAPType.dependentProductIds)
                {
                    PlayerPrefs.SetInt(
                        "IAPPurchased" + id,
                        1
                    );
                }
            }
            PlayerPrefs.Save();
            CheckBought();

        }

        private void OnPurchaseClick()
        {

            if (IAPManager.IAPInProgress)
                return;

            IAPManager.OnPurchaseInit(iAPType.productID);
        }
#endif


    }


}
