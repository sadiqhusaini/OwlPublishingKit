using System;
using System.Collections;
using UnityEngine;

namespace Owl.Monetization
{
#if USE_ADMOB
    // Shared native-load budget; an owner keeps its slot until the callback or disposal.
    public abstract class GoogleFullscreenAdOwner : MonoBehaviour
    {
        private sealed class Lease : IDisposable
        {
            private bool _released;
            private readonly int _epoch = _budgetEpoch;
            public void Dispose()
            {
                if (_released) return;
                _released = true;
                if (_epoch == _budgetEpoch) _activeLoads--;
            }
        }

        private static int _activeLoads;
        private static int _budgetEpoch;
        private static float _deferUntil;
        private const int MaximumConcurrentLoads = 2;
        private bool _disposed;
        private bool _loadPending;
        private bool _presenting;
        private int _generation;
        private Lease _lease;
        private Coroutine _waiting;
        private Action<int, Action> _request;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBudget()
        {
            _budgetEpoch++;
            _activeLoads = 0;
            _deferUntil = 0;
        }

        protected virtual void OnEnable()
        {
            Application.lowMemory += OnLowMemory;
            if (_request != null) QueueLoad(_request);
        }

        protected virtual void OnDisable()
        {
            Application.lowMemory -= OnLowMemory;
            if (_presenting) return;
            CancelLoad();
            DestroyAd();
        }

        protected static void InvokeSafely(Action action)
        {
            try { action?.Invoke(); }
            catch (Exception exception) { CustomDebug.LogWarning("Google ad listener failed: " + exception.Message); }
        }

        protected bool IsCurrentLoad(int generation) => !_disposed && generation == _generation;
        protected bool IsOwnerAlive => !_disposed;
        protected bool BeginPresentation()
        {
            if (_disposed || _presenting) return false;
            _presenting = true;
            return true;
        }
        protected bool EndPresentation()
        {
            if (!_presenting || _disposed) return false;
            _presenting = false;
            return true;
        }

        protected void QueueLoad(Action<int, Action> request)
        {
            if (_disposed || _loadPending || _presenting) return;
            _request = request;
            _loadPending = true;
            var generation = ++_generation;
            _waiting = StartCoroutine(WaitForBudget(generation, request));
        }

        private IEnumerator WaitForBudget(int generation, Action<int, Action> request)
        {
            // Yield once so even an immediate native callback cannot race coroutine assignment.
            yield return null;
            while (IsCurrentLoad(generation) &&
                   (_activeLoads >= MaximumConcurrentLoads || Time.realtimeSinceStartup < _deferUntil ||
                    !Utility.InternetManager.IsInternetAvailable || !Utility.ConsentManager.CanRequestAds))
                yield return new WaitForSecondsRealtime(0.25f);
            _waiting = null;
            if (!IsCurrentLoad(generation)) yield break;
            _activeLoads++;
            var lease = _lease = new Lease();
            Action complete = () =>
            {
                lease.Dispose();
                if (!IsCurrentLoad(generation)) return;
                _lease = null;
                _loadPending = false;
            };
            try { request(generation, complete); }
            catch (Exception exception)
            {
                complete();
                CustomDebug.LogWarning("Google fullscreen load failed: " + exception.Message);
                // Retry through the budget rather than leaving a placement permanently pending.
                _deferUntil = System.Math.Max(_deferUntil, Time.realtimeSinceStartup + 5f);
                QueueLoad(request);
            }
            yield return new WaitForSecondsRealtime(30f);
            if (IsCurrentLoad(generation) && _loadPending)
            {
                // A missing native callback must not occupy the shared budget forever.
                CustomDebug.LogWarning("Google fullscreen load timed out; retrying through the load budget.");
                CancelLoad();
                _deferUntil = System.Math.Max(_deferUntil, Time.realtimeSinceStartup + 5f);
                QueueLoad(request);
            }

        }

        private void CancelLoad()
        {
            ++_generation;
            if (_waiting != null) StopCoroutine(_waiting);
            _waiting = null;
            _lease?.Dispose();
            _lease = null;
            _loadPending = false;
        }

        private void OnLowMemory()
        {
            _deferUntil = System.Math.Max(_deferUntil, Time.realtimeSinceStartup + 30f);
            if (_presenting) return;
            CancelLoad();
            DestroyAd();
            if (_request != null) QueueLoad(_request);
        }

        protected virtual void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
            _disposed = true;
            CancelLoad();
            _request = null;
            DestroyAd();
        }

        protected abstract void DestroyAd();
    }
#endif
}
