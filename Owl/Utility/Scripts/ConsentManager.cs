using System;
using System.Collections;
using UnityEngine;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif
#if USE_ADMOB
using GoogleMobileAds.Ump.Api;
#endif

namespace Owl.Utility
{
    public class ConsentManager : MonoBehaviour
    {
        public static Action<bool> onConsentResolved;
        public static event Action OnCollectionConsentChanged;
        public static bool IsResolved { get; private set; }
        public static bool TrackingAuthorized { get; private set; }
        public static bool NonPersonalizedAds => !TrackingAuthorized;
        public static bool CanRequestAds { get; private set; }
        private static bool _analyticsGranted;
        private static bool _attributionGranted;
        private static bool _platformAllowsTracking;
        public static bool CanCollectAnalytics => IsResolved && _analyticsGranted;
        public static bool CanCollectAttribution => IsResolved && _attributionGranted && _platformAllowsTracking;
        private bool _started;
        private bool _finished;
        private int _generation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsResolved = TrackingAuthorized = CanRequestAds = false;
            _analyticsGranted = _attributionGranted = _platformAllowsTracking = false;
            onConsentResolved = null;
            OnCollectionConsentChanged = null;
        }

        /// <summary>Supply the user's current choices from your consent UI/CMP. Ad consent and ATT are separate.</summary>
        public static void SetCollectionConsent(bool analytics, bool attribution)
        {
            if (_analyticsGranted == analytics && _attributionGranted == attribution) return;
            _analyticsGranted = analytics;
            _attributionGranted = attribution;
            NotifyCollectionConsent();
        }

        public void Initialize()
        {
            if (_started) return;
            _started = true;
            IsResolved = TrackingAuthorized = CanRequestAds = false;
            _platformAllowsTracking = false;
            StartCoroutine(RunConsentFlow(++_generation));
        }

        private IEnumerator RunConsentFlow(int generation)
        {
            yield return GatherGdprConsent(generation);
            if (!IsCurrent(generation)) yield break;
            yield return RequestAtt(generation);
            if (IsCurrent(generation)) Complete();
        }

        private bool IsCurrent(int generation) => isActiveAndEnabled && !_finished && generation == _generation;

        /// <summary>Fail closed on timeout. Late native callbacks cannot enable ads or tracking.</summary>
        public void ResolveWithoutConsent()
        {
            if (_finished) return;
            _generation++;
            StopAllCoroutines();
            CanRequestAds = TrackingAuthorized = _platformAllowsTracking = false;
            Complete();
        }

        private void Complete()
        {
            if (_finished) return;
            _finished = true;
            IsResolved = true;
            NotifyCollectionConsent();
            var handlers = onConsentResolved;
            if (handlers != null)
                foreach (Action<bool> handler in handlers.GetInvocationList())
                {
                    try { handler(TrackingAuthorized); }
                    catch (Exception exception) { CustomDebug.LogWarning("Consent listener failed: " + exception.Message); }
                }
        }

        private static void NotifyCollectionConsent()
        {
            var handlers = OnCollectionConsentChanged;
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception exception) { CustomDebug.LogWarning("Collection consent listener failed: " + exception.Message); }
            }
        }

        private IEnumerator GatherGdprConsent(int generation)
        {
#if USE_ADMOB
            bool done = false;
            ConsentInformation.Update(new ConsentRequestParameters(), error =>
            {
                if (!IsCurrent(generation)) return;
                if (error != null)
                {
                    CustomDebug.LogWarning($"Consent update failed: {error.Message}");
                    CanRequestAds = ConsentInformation.CanRequestAds();
                    done = true;
                    return;
                }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (!IsCurrent(generation)) return;
                    if (formError != null) CustomDebug.LogWarning($"Consent form failed: {formError.Message}");
                    CanRequestAds = ConsentInformation.CanRequestAds();
                    done = true;
                });
            });
            while (!done && IsCurrent(generation)) yield return null;
#else
            // MAX projects must configure their provider's privacy choices separately.
            CanRequestAds = true;
            yield return null;
#endif
        }

        private IEnumerator RequestAtt(int generation)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();
                var deadline = Time.realtimeSinceStartup + 5f;
                while (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED &&
                       Time.realtimeSinceStartup < deadline && IsCurrent(generation))
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                    status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                }
            }
            if (!IsCurrent(generation)) yield break;
            TrackingAuthorized = status == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
            _platformAllowsTracking = TrackingAuthorized;
#else
            TrackingAuthorized = false; // ATT is not applicable; never represent this as an ATT grant.
            _platformAllowsTracking = true;
            yield return null;
#endif
        }

        private void OnDestroy()
        {
            _generation++;
        }
    }
}
