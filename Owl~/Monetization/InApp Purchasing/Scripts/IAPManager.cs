using System;
using UnityEngine;

#if USE_IAP
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine.Purchasing;
#endif

namespace Owl.Monetization
{

    public class IAPManager : MonoBehaviour
    {
        public static bool IsInitialized = false;
        public static bool IAPInProgress = false;
        public static string StoreDiagnosticState { get; private set; } = "Not started";
        public static string StoreDiagnosticError { get; private set; }
        public static int FetchedProductCount { get; private set; }

        private static GameObject _iapLoadingCanvas;

        public static IAPManager Instance;

#if USE_IAP
        private bool _isInitializing;
        private bool _restoreInProgress;
        private bool _purchasesFetchInProgress;
        private StoreController _storeController;
        public StoreController StoreController => _storeController;
#endif

        #region Delegates

        public delegate void IAPInitializedCallBack(bool status);
        public static IAPInitializedCallBack OnIAPInitialized;

#if USE_IAP
        public delegate void IAPProcessPurchaseCallBack(
            PendingOrder order);

        public static IAPProcessPurchaseCallBack OnIAPProcessPurchase;

        /// <summary>Register a persistent game service that grants every item in an order.
        /// Return true only after durable, transaction-idempotent fulfillment. Orders remain
        /// pending if no handler acknowledges them. The handler must tolerate store retries.</summary>
        public static Func<PendingOrder, bool> FulfillPurchase;

        public delegate void IAPPurchaseFailedCallBack(FailedOrder failedOrder);

        public static IAPPurchaseFailedCallBack OnIAPPurchaseFailed;

        public delegate void IAPRestoreSuccessFulCallBack();
        public static IAPRestoreSuccessFulCallBack OnIAPRestoreSuccessFul;

        public delegate void IAPRestoreFailedCallBack();
        public static IAPRestoreFailedCallBack OnIAPRestoreFailed;

        public delegate void IAPEntitlementRestoredCallBack(Product product);

        public static IAPEntitlementRestoredCallBack OnIAPEntitlementRestored;

#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsInitialized = false;
            IAPInProgress = false;
            StoreDiagnosticState = "Not started";
            StoreDiagnosticError = null;
            FetchedProductCount = 0;
            _iapLoadingCanvas = null;
            Instance = null;
            OnIAPInitialized = null;
#if USE_IAP
            OnIAPProcessPurchase = null;
            FulfillPurchase = null;
            OnIAPPurchaseFailed = null;
            OnIAPRestoreSuccessFul = null;
            OnIAPRestoreFailed = null;
            OnIAPEntitlementRestored = null;
#endif
        }

#if USE_IAP
        private const string MetaPurchaseTransactionsKey =
            "META_PURCHASE_TRANSACTIONS";

        private readonly HashSet<string> _metaPurchaseTransactions =
            new HashSet<string>();
#endif
        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
#if USE_IAP
                LoadMetaPurchaseTransactions();
#endif
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public static void Initialize(Transform parent)
        {
#if USE_IAP
            Instance ??= parent != null ? parent.GetComponent<IAPManager>() : null;
            if (Instance == null)
            {
                OnIAPInitialized?.Invoke(false);
                return;
            }

            var settings = OwlManager.OwlSettings;
            var loadingPrefab = settings != null && settings.showIapLoadingUi
                ? settings.OwlComponents?.IapLoadingCanvas
                : null;
            if (_iapLoadingCanvas == null && loadingPrefab != null)
                _iapLoadingCanvas = Instantiate(loadingPrefab, parent);

            StoreDiagnosticState = "Connecting";
            StoreDiagnosticError = null;

            _iapLoadingCanvas?.SetActive(false);

            IAPInProgress = false;
            Instance.InitializePurchasing();
#else
            OnIAPInitialized?.Invoke(false);
#endif
        }

#if USE_IAP
        public async void InitializePurchasing()
        {
            if (_isInitializing || IsInitialized)
            {
                CustomDebug.Log("IAP Already Initialized");
                return;
            }

            _isInitializing = true;
            StoreDiagnosticState = "Connecting to store";

            OwlManager.OwlStatusCanvas?.UpdateStatusInfo(
                "IAP: Initializing Started");

            try
            {
                await UnityServices.InitializeAsync();
                if (!this || Instance != this) return;

                DetachStoreCallbacks();
                _storeController = UnityIAPServices.StoreController();

                _storeController.OnStoreDisconnected += OnStoreDisconnected;
                _storeController.OnStoreConnected += OnStoreConnected;

                _storeController.OnProductsFetched += OnProductsFetched;
                _storeController.OnProductsFetchFailed += OnProductsFetchFailed;

                _storeController.OnPurchasesFetched += OnPurchasesFetched;
                _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

                _storeController.OnPurchasePending += OnPurchasePending;
                _storeController.OnPurchaseFailed += OnPurchaseFailed;
                _storeController.OnPurchaseDeferred += OnPurchaseDeferred;

                await _storeController.Connect();

            }
            catch (Exception e)
            {
                if (!this || Instance != this) return;
                _isInitializing = false;
                IsInitialized = false;
                StoreDiagnosticState = "Initialization failed";
                StoreDiagnosticError = e.GetBaseException().Message;

                Debug.LogError(
                    $"IAP Initialization Failed: {e}"
                );

                OnIAPInitialized?.Invoke(false);
            }
        }

        private void DetachStoreCallbacks()
        {
            if (_storeController == null) return;
            _storeController.OnStoreDisconnected -= OnStoreDisconnected;
            _storeController.OnStoreConnected -= OnStoreConnected;
            _storeController.OnProductsFetched -= OnProductsFetched;
            _storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
            _storeController.OnPurchasesFetched -= OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _storeController.OnPurchasePending -= OnPurchasePending;
            _storeController.OnPurchaseFailed -= OnPurchaseFailed;
            _storeController.OnPurchaseDeferred -= OnPurchaseDeferred;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            DetachStoreCallbacks();
            _storeController = null;
            Instance = null;
            IsInitialized = false;
            FinishPurchaseUI();
        }

        private void OnStoreConnected()
        {
            StoreDiagnosticState = "Fetching products";
            StoreDiagnosticError = null;
            var catalog = ProductCatalog.LoadDefaultCatalog();
            var catalogProvider = CodelessCatalogProvider.PopulateCatalogProvider(catalog);
            catalogProvider.FetchProducts(products => _storeController.FetchProducts(products));
        }

        private void FetchPurchasesSafe()
        {
            if (_storeController == null)
                return;

            if (_purchasesFetchInProgress)
            {
                CustomDebug.Log("IAP FetchPurchases already in progress");
                return;
            }

            _purchasesFetchInProgress = true;

            CustomDebug.Log("IAP FetchPurchases START");

            _storeController.FetchPurchases();
        }



        private void OnStoreDisconnected(
            StoreConnectionFailureDescription failure)
        {
            _isInitializing = false;
            IsInitialized = false;
            StoreDiagnosticState = "Disconnected";
            StoreDiagnosticError = failure.message;

            Debug.LogError(
                $"IAP Store Disconnected: {failure.message}"
            );

            OnIAPInitialized?.Invoke(false);
        }

        private void OnProductsFetched(List<Product> products)
        {
            FetchedProductCount = products != null ? products.Count : 0;
            StoreDiagnosticState = "Products loaded";
            StoreDiagnosticError = null;
            CustomDebug.Log($"IAP Products Fetched: {FetchedProductCount}");

            foreach (var product in products)
            {
                CustomDebug.Log(
                    $"IAP Product: {product.definition.id} | " +
                    $"Type: {product.definition.type}"
                );
            }

            // IAP is ready as soon as products are available.
            _isInitializing = false;
            IsInitialized = true;

            OwlManager.OwlStatusCanvas?.UpdateStatusInfo(
                "IAP: Initializing Completed"
            );

            OnIAPInitialized?.Invoke(true);

#if UNITY_ANDROID

            // Restore existing purchases in the background.
            StoreDiagnosticState = "Fetching purchases";
            FetchPurchasesSafe();

#elif UNITY_IOS

    // Do NOT restore automatically on iOS.
    // User must press Restore.

#endif
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            StoreDiagnosticState = "Product fetch failed";
            StoreDiagnosticError = failure.ToString();
            Debug.LogError(
                $"IAP Product Fetch Failed: {failure}"
            );
            _isInitializing = false;
            IsInitialized = false;
            OnIAPInitialized?.Invoke(false);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            _purchasesFetchInProgress = false;
            StoreDiagnosticState = "Ownership resolved";

            CustomDebug.Log("IAP Purchases Fetched");

            if (orders != null)
            {
                foreach (var order in orders.ConfirmedOrders)
                {
                    if (order == null)
                        continue;

                    foreach (var item in order.CartOrdered.Items())
                    {
                        Product product = item.Product;

                        if (product == null)
                            continue;

                        if (product.definition.type != ProductType.NonConsumable &&
                            product.definition.type != ProductType.Subscription)
                            continue;

                        CustomDebug.Log(
                            $"IAP Entitlement Restored: {product.definition.id}"
                        );

                        OnIAPEntitlementRestored?.Invoke(product);
                    }
                }
            }

            if (_restoreInProgress)
            {
                _restoreInProgress = false;
                IAPRestoreSuccessFul();
            }
        }

        private void OnPurchasesFetchFailed(
            PurchasesFetchFailureDescription failure)
        {
            _purchasesFetchInProgress = false;
            StoreDiagnosticState = "Ownership fetch failed";
            StoreDiagnosticError = failure.ToString();

            Debug.LogError(
                $"IAP Purchases Fetch Failed: {failure}"
            );

            if (_restoreInProgress)
            {
                _restoreInProgress = false;
                IAPRestoreFailed();
            }
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (order == null)
            {
                Debug.LogError("IAP PendingOrder is NULL");
                return;
            }

            var items = order.CartOrdered.Items();

            if (items == null || items.Count == 0)
            {
                Debug.LogError(
                    "IAP PendingOrder contains no products"
                );

                FinishPurchaseUI();
                return;
            }

            string transactionId = order.Info.TransactionID;

            if (string.IsNullOrEmpty(transactionId))
            {
                Debug.LogError(
                    "IAP Purchase has no transaction ID"
                );

                FinishPurchaseUI();
                return;
            }

            CustomDebug.Log(
                $"IAP Purchase Pending | " +
                $"Transaction: {transactionId}"
            );

            // Never acknowledge an order just because a UI or analytics listener exists.
            // The game's durable fulfillment service owns idempotency by transaction ID.
            try
            {
                var removeAdsIds = OwlManager.OwlSettings?.noAdsIds;
                bool builtInRemoveAds = removeAdsIds != null;
                foreach (var item in items)
                {
                    var id = item?.Product?.definition?.id;
                    if (string.IsNullOrEmpty(id) || Array.IndexOf(removeAdsIds ?? Array.Empty<string>(), id) < 0)
                    {
                        builtInRemoveAds = false;
                        break;
                    }
                }
                if (builtInRemoveAds)
                {
                    foreach (var item in items) OwlManager.ApplyRemoveAdsEntitlement(item.Product.definition.id);
                }
                else if (FulfillPurchase == null || !FulfillPurchase(order))
                {
                    Debug.LogWarning("IAP purchase remains pending: no fulfillment service acknowledged it.");
                    FinishPurchaseUI();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"IAP Purchase Fulfillment Exception: {e}");
                FinishPurchaseUI();
                return;
            }

            // Observers update UI and analytics; they do not decide store acknowledgement.
            try { OnIAPProcessPurchase?.Invoke(order); }
            catch (Exception e) { Debug.LogError($"IAP purchase observer failed: {e}"); }

            // Confirm ONLY after fulfillment succeeds.
            try
            {
                _storeController.ConfirmPurchase(order);
            }
            catch (Exception e)
            {
                Debug.LogError($"IAP Purchase Confirmation Failed: {e}");
                FinishPurchaseUI();
                return;
            }

            CustomDebug.Log(
                $"IAP Purchase Confirmed | " +
                $"Transaction: {transactionId}"
            );

            FinishPurchaseUI();

            // Analytics AFTER store confirmation.
            foreach (var item in items)
            {
                Product product = item.Product;

                if (product == null)
                    continue;

                CustomDebug.Log(
                    $"IAP Analytics | " +
                    $"Product: {product.definition.id} | " +
                    $"Transaction: {transactionId}"
                );

#if USE_FIREBASEANALYTICS && UNITY_IOS

                Firebase.Analytics.FirebaseAnalytics
                    .LogAppleTransactionAsync(transactionId);

#endif

                if (product.metadata == null)
                    continue;

                if (HasMetaPurchaseBeenSent(transactionId))
                {
                    CustomDebug.Log(
                        $"Facebook Purchase SKIPPED | " +
                        $"Transaction: {transactionId}"
                    );

                    continue;
                }

                float amount =
                    (float)product.metadata.localizedPrice;

                string currency =
                    product.metadata.isoCurrencyCode;

#if USE_FACEBOOK && USE_FACEBOOK_SDK
                Owl.Analytics.FacebookEvents.Purchase(
                    amount,
                    currency
                );

                MarkMetaPurchaseAsSent(transactionId);

                CustomDebug.Log(
                    $"Facebook Purchase Sent | " +
                    $"Product: {product.definition.id} | " +
                    $"Transaction: {transactionId} | " +
                    $"Amount: {amount} | " +
                    $"Currency: {currency}"
                );
#endif
            }
        }

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            if (failedOrder == null)
            {
                Debug.LogError(
                    "IAP Purchase Failed: FailedOrder is NULL"
                );

                FinishPurchaseUI();
                return;
            }

            CustomDebug.Log(
                $"IAP Purchase Failed: {failedOrder}"
            );

            try
            {
                OnIAPPurchaseFailed?.Invoke(failedOrder);
            }
            finally
            {
                FinishPurchaseUI();
            }
        }

        private void OnPurchaseDeferred(DeferredOrder deferredOrder)
        {
            if (deferredOrder == null)
            {
                FinishPurchaseUI();
                return;
            }

            CustomDebug.Log(
                $"IAP Purchase Deferred: {deferredOrder}"
            );

            // Purchase is waiting for approval.
            // Don't confirm it.
            // Don't treat it as a failure.

            FinishPurchaseUI();
        }

        public static void OnPurchaseInit(string pId)
        {
            if (!OwlManager.IsOwlManager)
                return;

            if (!IsInitialized)
                return;

            if (Instance == null)
                return;

            if (IAPInProgress)
                return;

            _iapLoadingCanvas?.SetActive(true);
            IAPInProgress = true;

            Instance.PurchaseProduct(pId);
        }

        public static void OnRestore()
        {
            if (!OwlManager.IsOwlManager)
                return;

            if (!IsInitialized)
                return;

            if (Instance == null)
                return;

            if (Instance._restoreInProgress)
                return;

            if (IAPInProgress)
                return;

            Instance._restoreInProgress = true;

            _iapLoadingCanvas?.SetActive(true);
            IAPInProgress = true;

            Instance.RestorePurchases();
        }

        public static StoreController GetStoreController()
        {
            return Instance?._storeController;
        }

        private void PurchaseProduct(string pId)
        {
            if (_storeController == null)
            {
                Debug.LogError("IAP StoreController is NULL");
                FinishPurchaseUI();
                return;
            }

            var product = _storeController.GetProductById(pId);

            if (product == null)
            {
                Debug.LogError(
                    $"IAP Product not found: {pId}"
                );

                FinishPurchaseUI();
                return;
            }

            CustomDebug.Log(
                $"IAP Purchasing Product: {product.definition.id}"
            );

            try
            {
                _storeController.PurchaseProduct(product);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"IAP PurchaseProduct Exception: {e}"
                );

                FinishPurchaseUI();
            }
        }

        private void RestorePurchases()
        {
            CustomDebug.Log("IAP-----------OnRestore");

            if (_storeController == null)
            {
                Debug.LogError("IAP StoreController is NULL");

                _restoreInProgress = false;

                IAPRestoreFailed();
                return;
            }

#if UNITY_IOS
            _storeController.RestoreTransactions(
                (success, error) =>
                {
                    if (success)
                    {
                        CustomDebug.Log(
                            "IAP Apple Restore Request Successful"
                        );

                        // Important:
                        // This only means the Apple restore request succeeded.
                        // The actual restored products are handled by
                        // OnPurchasesFetched().
                    }
                    else
                    {
                        CustomDebug.Log(
                            $"IAP Apple Restore Failed: {error}"
                        );

                        _restoreInProgress = false;

                        IAPRestoreFailed();
                    }
                }
            );
#else
            FetchPurchasesSafe();
#endif
        }

        #region Unity IAP Callbacks



        private void LoadMetaPurchaseTransactions()
        {
            string saved = PlayerPrefs.GetString(
                MetaPurchaseTransactionsKey,
                string.Empty
            );

            if (string.IsNullOrEmpty(saved))
                return;

            string[] transactions = saved.Split('|');

            foreach (string transactionId in transactions)
            {
                if (!string.IsNullOrEmpty(transactionId))
                    _metaPurchaseTransactions.Add(transactionId);
            }
        }

        private bool HasMetaPurchaseBeenSent(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return false;

            return _metaPurchaseTransactions.Contains(transactionId);
        }

        private void MarkMetaPurchaseAsSent(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return;

            _metaPurchaseTransactions.Add(transactionId);

            PlayerPrefs.SetString(
                MetaPurchaseTransactionsKey,
                string.Join("|", _metaPurchaseTransactions)
            );

            PlayerPrefs.Save();
        }



        private void FinishPurchaseUI()
        {
            _iapLoadingCanvas?.SetActive(false);

            IAPInProgress = false;
        }

        #endregion

        #region Static Events

        public static void IAPRestoreSuccessFul()
        {
            OnIAPRestoreSuccessFul?.Invoke();

            _iapLoadingCanvas?.SetActive(false);

            IAPInProgress = false;
        }

        public static void IAPRestoreFailed()
        {
            OnIAPRestoreFailed?.Invoke();

            _iapLoadingCanvas?.SetActive(false);

            IAPInProgress = false;
        }

        #endregion
#else
        public static void OnPurchaseInit(string productId) { }
        public static void OnRestore() { }
#endif
    }

#if USE_IAP
    [System.Serializable]
    public class IAPType
    {
        public string productID;
        public ProductType productType;
        [Space(10)]
        public string[] dependentProductIds;

        public IAPType(string pID, ProductType pType)
        {
            productID = pID;
            productType = pType;
        }
    }
#endif
}
