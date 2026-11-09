using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace Owl
{
    public class SplashController : MonoBehaviour
    {
        private Coroutine fakeProgressCo;
        private Coroutine loadSceneCo;
        private Coroutine loadingTextCo;
        private Coroutine appOpenWaitCo;

        [SerializeField] private bool waitForAppOpen;
        [SerializeField, Min(0f)] private float appOpenWaitTimeout = 8f;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text loadingText;

        private bool sdkReady;
        private bool sdkInitialized;
        private bool appOpenResolved;
        private bool waitingForAppOpenClose;
        private bool started;

        private void Start()
        {
            fillImage.fillAmount = 0f;

            fakeProgressCo = StartCoroutine(FakeProgress());
            loadingTextCo = StartCoroutine(AnimatedLoadingText());
            loadSceneCo = StartCoroutine(LoadScene());
            started = true;
            sdkInitialized |= OwlManager.OwlInitialized;
            if (OwlManager.AppOpenInitializationResolved)
                OnAppOpenInitialized();
            TryContinue();
        }

        private void OnEnable()
        {
            OwlManager.OnSDKInitialized += OnSDKInitialized;
            OwlManager.OnAppOpenInitializedResult += OnAppOpenInitialized;
            Monetization.AdManager.OnAdOpenedEvent += OnAdOpened;
            Monetization.AdManager.OnAdClosedEvent += OnAdClosed;
            sdkInitialized |= OwlManager.OwlInitialized;
            if (OwlManager.AppOpenInitializationResolved)
                OnAppOpenInitialized();
            TryContinue();
        }

        private void OnDisable()
        {
            OwlManager.OnSDKInitialized -= OnSDKInitialized;
            OwlManager.OnAppOpenInitializedResult -= OnAppOpenInitialized;
            Monetization.AdManager.OnAdOpenedEvent -= OnAdOpened;
            Monetization.AdManager.OnAdClosedEvent -= OnAdClosed;
            if (appOpenWaitCo != null)
            {
                StopCoroutine(appOpenWaitCo);
                appOpenWaitCo = null;
            }
        }

        private void OnSDKInitialized()
        {
            sdkInitialized = true;
            TryContinue();
        }

        private void OnAppOpenInitialized()
        {
            // Loading an App Open placement only means it is ready to show. Keep
            // the splash gate closed until its presentation has completed.
            if (!appOpenResolved && !waitingForAppOpenClose && ShouldWaitForAppOpen() &&
                Monetization.AdManager.IsAppOpenAvailable())
            {
                RestartAppOpenWait();
            }

            TryContinue();
        }

        private void OnAdOpened()
        {
            if (!waitForAppOpen) return;

            waitingForAppOpenClose = true;
            StopAppOpenWait();
        }

        private void OnAdClosed()
        {
            if (!waitingForAppOpenClose) return;

            waitingForAppOpenClose = false;
            appOpenResolved = true;
            TryContinue();
        }

        private void RestartAppOpenWait()
        {
            if (waitingForAppOpenClose || appOpenWaitTimeout <= 0f) return;
            StopAppOpenWait();
            appOpenWaitCo = StartCoroutine(WaitForAppOpen());
        }

        private void StopAppOpenWait()
        {
            if (appOpenWaitCo == null) return;
            StopCoroutine(appOpenWaitCo);
            appOpenWaitCo = null;
        }

        private bool ShouldWaitForAppOpen()
        {
#if USE_ADMOB || USE_MAXADS
            var settings = OwlManager.OwlSettings;
            return waitForAppOpen && !OwlBootstrap.StartupDegraded && settings != null && Monetization.AdManager.IsInitialized &&
                   Monetization.AdManager.IsAppOpenEnabled &&
                   settings.mediation != Mediation.NoAds &&
                   Utility.ConsentManager.CanRequestAds && !Monetization.AdManager.IsRemoveAdsPurchased();
#else
            return false;
#endif
        }

        private void TryContinue()
        {
            if (!started || !sdkInitialized || sdkReady || waitingForAppOpenClose) return;
            if (ShouldWaitForAppOpen() && !appOpenResolved && appOpenWaitTimeout > 0f)
            {
                appOpenWaitCo ??= StartCoroutine(WaitForAppOpen());
                return;
            }
            FinishLoading();
        }

        private IEnumerator WaitForAppOpen()
        {
            var settings = OwlManager.OwlSettings;
            var startupDelay = settings != null ? settings.appOpenStartDelay +
                (settings.showAdComingUi ? 3f * settings.adComingWaitStep : 0f) : 0f;
            yield return new WaitForSecondsRealtime(appOpenWaitTimeout + startupDelay);
            appOpenWaitCo = null;
            if (waitingForAppOpenClose) yield break;
            appOpenResolved = true;
            CustomDebug.LogWarning("Splash app-open wait timed out; continuing to the game.");
            FinishLoading();
        }

        private void FinishLoading()
        {
            if (sdkReady || waitingForAppOpenClose) return;
            sdkReady = true;
            if (appOpenWaitCo != null)
            {
                StopCoroutine(appOpenWaitCo);
                appOpenWaitCo = null;
            }
            if (fakeProgressCo != null)
                StopCoroutine(fakeProgressCo);

            Analytics.AnalyticsManager.TrackDesignEvent("OnSDKInitialized");
            if (loadSceneCo == null)
                loadSceneCo = StartCoroutine(LoadScene());
        }

        // ---------------------------------------------
        // Fake progress (non-linear, organic)
        // ---------------------------------------------
        private IEnumerator FakeProgress()
        {
            float progress = fillImage.fillAmount;

            while (!sdkReady && progress < 0.5f)
            {
                // Ease-out + Perlin noise for organic feel
                float baseSpeed = Mathf.Lerp(0.6f, 0.2f, progress * 2f);
                float noise = Mathf.PerlinNoise(Time.unscaledTime * 0.8f, 0f) * 0.15f;

                progress += (baseSpeed + noise) * Time.unscaledDeltaTime;
                fillImage.fillAmount = Mathf.Min(progress, 0.5f);

                yield return null;
            }
        }

        // ---------------------------------------------
        // Real async scene loading
        // ---------------------------------------------
        private IEnumerator LoadScene()
        {
            Analytics.AnalyticsManager.TrackDesignEvent("OwlSplashLoading");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);
            asyncLoad.allowSceneActivation = false;

            // 50% → 90%
            while (asyncLoad.progress < 0.9f)
            {
                float target = 0.5f + asyncLoad.progress * 0.4f;
                fillImage.fillAmount = Mathf.MoveTowards(
                    fillImage.fillAmount,
                    target,
                    Time.unscaledDeltaTime
                );
                yield return null;
            }

            while (!sdkReady) yield return null;

            // 90% → 100% (smooth final polish)
            while (fillImage.fillAmount < 1f)
            {
                fillImage.fillAmount = Mathf.MoveTowards(
                    fillImage.fillAmount,
                    1f,
                    Time.unscaledDeltaTime * 0.6f
                );
                yield return null;
            }

            // An ad can start after startup resolves, including during the final
            // progress animation. Recheck immediately before activating the scene.
            while (!sdkReady || waitingForAppOpenClose) yield return null;
            asyncLoad.allowSceneActivation = true;
        }

        // ---------------------------------------------
        // Animated loading text (non-repeating)
        // ---------------------------------------------
        private IEnumerator AnimatedLoadingText()
        {
            string[] phrases =
            {
                "Loading",
                "Preparing Assets",
                "Almost There"
            };

            int phraseIndex = 0;
            int dots = 0;
            float phraseTimer = 0f;
            float phraseDuration = 2.5f; // seconds per phrase
            Color baseColor = loadingText.color;

            while (fillImage.fillAmount < 0.9f)
            {
                // Animate dots
                dots = (dots + 1) % 4;
                loadingText.text = phrases[phraseIndex] + new string('.', dots);

                // Breathing alpha
                float alpha = 0.7f + Mathf.PingPong(Time.unscaledTime * 0.6f, 0.3f);
                loadingText.color = new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    alpha
                );

                // Change phrase based on timer
                phraseTimer += 0.25f;
                if (phraseTimer >= phraseDuration)
                {
                    phraseTimer = 0f;
                    phraseIndex = (phraseIndex + 1) % phrases.Length;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }

            // Final message
            loadingText.color = baseColor;
            loadingText.text = "Almost Ready";
        }
    }
}
