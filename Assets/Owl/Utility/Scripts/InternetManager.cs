using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Owl.Utility
{
    public class InternetManager : MonoBehaviour
    {
        public static bool IsInternetAvailable { get; private set; }
        public static int ConsecutiveFailures { get; private set; }

        private GameObject _noInternetCanvas;

        public static Action<bool> OnOnlineCheckComplete;

        private bool _initialized;
        private bool _paused;
        private int _failures;
        private UnityEngine.Networking.UnityWebRequest _request;
        public const float OnlineCheckInterval = 30f;
        public const float MaximumRetryInterval = 60f;
        private bool _showUI;
        private bool _allowOffline;

        private Coroutine _coroutine;
        private Coroutine _timerCoroutine;
        private bool _firstTime;

        private bool _internetStatus;
        private bool _lastInternetStatus;

        private float _savedTimeScale = 1;



        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            if (_noInternetCanvas==null)
            {
                _showUI = OwlManager.OwlSettings.showNoInternetUi;
                var noAdCanvasRef = OwlManager.OwlSettings?.OwlComponents.NoInternetCanvas;
                if (noAdCanvasRef)
                {
                    _noInternetCanvas = Instantiate(noAdCanvasRef, OwlManager.GetCanvasesParent());
                    _noInternetCanvas.name = noAdCanvasRef?.name;
                    _noInternetCanvas.GetComponent<Canvas>().enabled = _showUI;
                    _noInternetCanvas.GetComponent<CanvasScaler>().enabled = _showUI;
                    _noInternetCanvas.GetComponent<GraphicRaycaster>().enabled = _showUI;
                }
            }
            _lastInternetStatus = _internetStatus = IsInternetAvailable = false;
            ConsecutiveFailures = 0;
            _firstTime = true;
            _savedTimeScale = Time.timeScale;
            if (!_paused && isActiveAndEnabled) _coroutine = StartCoroutine(CheckRoutine());
        }

        /// <summary>Release the offline overlay while retaining connectivity monitoring.</summary>
        public void ContinueOffline()
        {
            _allowOffline = true;
            _noInternetCanvas?.SetActive(false);
        }

        private System.Collections.IEnumerator CheckRoutine()
        {
            while (true)
            {
                _timerCoroutine = StartCoroutine(InternetCheck());
                yield return _timerCoroutine;
                _timerCoroutine = null;
                _failures = _internetStatus ? 0 : System.Math.Min(_failures + 1, 5);
                ConsecutiveFailures = _failures;
                var interval = _internetStatus ? OnlineCheckInterval :
                    System.Math.Min(MaximumRetryInterval, 5f * (float)System.Math.Pow(2, _failures - 1));
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private System.Collections.IEnumerator GoogleBlankCheck()
        {
            const string url = "https://www.google.com/blank.html";

            using var www = UnityEngine.Networking.UnityWebRequest.Get(URLAntiCacheRandomizer(url));

            _request = www;
            www.timeout = 10;
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            _request = null;

#if UNITY_2020_1_OR_NEWER
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.ProtocolError && www.result != UnityEngine.Networking.UnityWebRequest.Result.ConnectionError)
#else
            if (!www.isHttpError && !www.isNetworkError)
#endif
            {
                _internetStatus= www.downloadedBytes == 0;
            }
            else
            {
            }
        }

        private System.Collections.IEnumerator InternetCheck()
        {
            _internetStatus = false;
            yield return GoogleBlankCheck();

            if (_lastInternetStatus !=_internetStatus || _firstTime)
            {
                if (_firstTime)
                {
                    _firstTime = false;
                }

                _lastInternetStatus=_internetStatus;

                onInternetCheckComplete(_lastInternetStatus);
            }
        }


        private static string URLAntiCacheRandomizer(string url)
        {
            return $"{url}?p={Random.Range(1, int.MaxValue)}";
        }

        private void onInternetCheckComplete(bool isConnected)
        {
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo($"OnInternetCheckComplete: {isConnected}");
            _noInternetCanvas?.SetActive(!isConnected && !_allowOffline);
            IsInternetAvailable = isConnected;
            OnOnlineCheckComplete?.Invoke(isConnected);
        }

        private void StopChecks()
        {
            var request = _request;
            _request = null;
            request?.Abort();
            if (_coroutine != null) StopCoroutine(_coroutine);
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _coroutine = _timerCoroutine = null;
            request?.Dispose();
        }

        private void OnEnable()
        {
            if (_initialized && !_paused && _coroutine == null)
                _coroutine = StartCoroutine(CheckRoutine());
        }

        private void OnDisable() => StopChecks();

        private void OnApplicationPause(bool paused)
        {
            _paused = paused;
            StopChecks();
            if (!paused && _initialized && isActiveAndEnabled)
                _coroutine = StartCoroutine(CheckRoutine());
        }

        private void OnApplicationQuit() => StopChecks();
    }
}
