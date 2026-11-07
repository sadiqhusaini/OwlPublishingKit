using UnityEngine;
#if USE_IAP
using UnityEngine.Purchasing;
#endif

namespace Owl
{
    public class OwlManager : MonoBehaviour
    {
        private static OwlManager _instance=null;
        private static OwlSettings _owlSettings;
        public static OwlSettings OwlSettings
        {
            get
            {
                if (_owlSettings == null)
                    _owlSettings = Resources.Load<OwlSettings>("OwlSettings");

                return _owlSettings;
            }
        }
        public static OwlStatusScript OwlStatusCanvas;
        /// <summary>Gameplay startup resolved, possibly with fallback services. Not an ad-load or purchase-readiness guarantee.</summary>
        public static bool OwlInitialized { get; private set; } = false;
        public static bool AppOpenInitializationResolved { get; private set; }
        public static bool HasRemoveAdsEntitlement { get; private set; }
        
        public static System.Action OnSDKInitialized;
        public static System.Action OnAppOpenInitializedResult;



        private static GameObject _canvasesGameObject;
        
        private  OwlBootstrap _bootstrap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _owlSettings = null;
            _canvasesGameObject = null;
            OwlStatusCanvas = null;
            OwlInitialized = AppOpenInitializationResolved = false;
            HasRemoveAdsEntitlement = false;
            OnSDKInitialized = OnAppOpenInitializedResult = null;
        }

        private void Awake()
        {
            if (_instance == null)
            {
                DontDestroyOnLoad(gameObject);
                _instance = this;
                OwlInitialized = false;
                AppOpenInitializationResolved = false;

                _bootstrap = gameObject.GetComponent<OwlBootstrap>() ?? gameObject.AddComponent<OwlBootstrap>();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        

        private void Start()
        {
            if (_instance != this) return;
            if (OwlSettings.screenSDKInfo)
            {
                OwlStatusCanvas ??= OwlSettings.OwlComponents.SpawnOwlStatusCanvas();
                OwlStatusCanvas?.ShowStatusInfo();
            }
            
            if (OwlSettings.autoSDKInit)
                Initialize();
        }
   
   

        /// <summary>Start Owl manually when Automatic startup is disabled. Safe to call repeatedly.</summary>
        public static void Initialize()
        {
            if (_instance == null)
            {
                CustomDebug.LogWarning("Owl initialization requires an active OwlManager in the scene.");
                return;
            }
            _instance._bootstrap.Initialize();
        }

#if USE_IAP
        private void OnEnable()
        {
            if (_instance == this)
            {
                Monetization.IAPManager.OnIAPProcessPurchase += OnIAPPurchaseComplete;
                Monetization.IAPManager.OnIAPEntitlementRestored += OnIAPEntitlementRestored;
            }
        }

        private void OnDisable()
        {
            Monetization.IAPManager.OnIAPProcessPurchase -= OnIAPPurchaseComplete;
            Monetization.IAPManager.OnIAPEntitlementRestored -= OnIAPEntitlementRestored;
        }

        private void OnIAPPurchaseComplete(UnityEngine.Purchasing.PendingOrder order)
        {
            if (order == null || order.CartOrdered == null)
                return;

            foreach (var item in order.CartOrdered.Items())
            {
                if (item?.Product != null)
                    ApplyRemoveAdsEntitlement(item.Product.definition.id);
            }
        }

        private void OnIAPEntitlementRestored(UnityEngine.Purchasing.Product product)
        {
            if (product != null)
                ApplyRemoveAdsEntitlement(product.definition.id);
        }

        private static void ApplyRemoveAdsEntitlement(string productId)
        {
            var removeAdsIds = OwlSettings?.noAdsIds;
            if (removeAdsIds == null || string.IsNullOrEmpty(productId))
                return;

            foreach (var id in removeAdsIds)
            {
                if (productId != id)
                    continue;

                HasRemoveAdsEntitlement = true;

                Monetization.AdManager.PurchaseRemoveAds();
                return;
            }
        }
#endif
   
       
        
        
        public static void SDKInitialized()
        {
            if (OwlInitialized) return;
            OwlStatusCanvas?.UpdateStatusInfo("SDKInitialized");
            OwlInitialized = true;
            var handlers = OnSDKInitialized;
            if (handlers == null) return;
            foreach (System.Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (System.Exception exception) { CustomDebug.LogWarning("Owl Ready listener failed: " + exception.Message); }
            }
        }
        public static void AppOpenResult(bool value)
        {
            AppOpenInitializationResolved = true;
            OwlStatusCanvas?.UpdateStatusInfo("SDKInitialized-AppOpenResult "+value);
            OnAppOpenInitializedResult?.Invoke();
        }
       
        public static bool IsOwlManager
        {
            get
            {
                if (_instance != null) 
                    return true;

                return false;
            }
        }

        public static Transform GetCanvasesParent()
        {
            if (_canvasesGameObject == null) _canvasesGameObject = new GameObject("Canvases");
            _canvasesGameObject.transform.parent = _instance.transform;
            return _canvasesGameObject.transform;
        }

        public static MonoBehaviour[] GetDebugComponents()
        {
            return _instance != null
                ? _instance.GetComponentsInChildren<MonoBehaviour>(true)
                : System.Array.Empty<MonoBehaviour>();
        }
    }
}
