using UnityEngine;
using System;

#if USE_IAP
using UnityEngine.Purchasing;
#endif

namespace Owl.Monetization
{
    public class IAPOfferPanelScript : MonoBehaviour
    {
#if USE_IAP

        public IAPType iAPType =
            new IAPType("removeAds", ProductType.NonConsumable);

        private bool IsPurchased
        {
            get => PlayerPrefs.GetInt(
                "IAPPurchased" + iAPType.productID,
                0) == 1;

            set => PlayerPrefs.SetInt(
                "IAPPurchased" + iAPType.productID,
                value ? 1 : 0);
        }

#endif

        [Space(20)]
        public UnityEngine.Events.UnityEvent onPurchaseCompleted;
        public UnityEngine.Events.UnityEvent onPurchaseFailed;

        public GameObject panel;

        private Action _onComplete;

        private void OnEnable()
        {
#if USE_IAP

            IAPManager.OnIAPProcessPurchase += OnIAPPurchaseComplete;
            IAPManager.OnIAPPurchaseFailed += OnIAPPurchaseFailed;
            IAPManager.OnIAPEntitlementRestored += OnIAPEntitlementRestored;

            CheckBought();

#endif
        }



        private void OnDisable()
        {
#if USE_IAP

            IAPManager.OnIAPProcessPurchase -= OnIAPPurchaseComplete;
            IAPManager.OnIAPPurchaseFailed -= OnIAPPurchaseFailed;
            IAPManager.OnIAPEntitlementRestored -= OnIAPEntitlementRestored;

#endif
        }

        public void ShowIAPPanel()
        {
            if (panel != null)
                panel.SetActive(true);
        }

        public void ShowIAPPanel(Action onComplete)
        {
            _onComplete = onComplete;

            if (panel != null)
                panel.SetActive(true);
        }

        public void CloseIAPPanel()
        {
            if (panel != null)
                panel.SetActive(false);

            _onComplete?.Invoke();
            _onComplete = null;
        }

#if USE_IAP

        private void OnIAPPurchaseComplete(PendingOrder order)
        {
            if (order == null)
                return;

            foreach (var item in order.CartOrdered.Items())
            {
                Product product = item.Product;

                if (product == null)
                    continue;

                if (product.definition.id != iAPType.productID)
                    continue;

                // Purchase matched.
                onPurchaseCompleted?.Invoke();

                if (iAPType.productType == ProductType.NonConsumable)
                {
                    SetBought();
                }

                _onComplete?.Invoke();
                _onComplete = null;

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
                $"IAP Offer Entitlement Restored: {product.definition.id}"
            );

            SetBought();
        }


        private void OnIAPPurchaseFailed(FailedOrder failedOrder)
        {
            if (failedOrder == null)
                return;

            foreach (var item in failedOrder.CartOrdered.Items())
            {
                var product = item.Product;
                if (product == null || product.definition.id != iAPType.productID)
                    continue;

                onPurchaseFailed?.Invoke();

                _onComplete?.Invoke();
                _onComplete = null;
                return;
            }
        }


        private void SetBought()
        {
            IsPurchased = true;

            PlayerPrefs.Save();

            CheckBought();
        }

        private void CheckBought()
        {
            if (IsPurchased)
            {
                gameObject.SetActive(false);
            }
        }

#endif
    }
}
