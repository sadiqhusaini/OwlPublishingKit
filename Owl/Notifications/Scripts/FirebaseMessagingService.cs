using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if USE_FIREBASEMESSAGING
using Firebase.Messaging;
#endif

namespace Owl.Notifications
{
    /// <summary>Optional FCM client. Call public methods from Unity's main thread.</summary>
    public sealed class FirebaseMessagingService : MonoBehaviour
    {
        private static FirebaseMessagingService _instance;
        private static readonly Regex TopicPattern = new Regex(@"\A[a-zA-Z0-9\-_.~%]{1,900}\z");
        private bool _initialized;
        private bool _subscribed;
        private int _generation;
        private CancellationTokenSource _lifetime;
        private string _token;
        private Task<string> _tokenTask;
        private Task _permissionTask;
#if UNITY_ANDROID && !UNITY_EDITOR && USE_FIREBASEMESSAGING
        private TaskCompletionSource<bool> _androidPermission;
#endif

        public static bool IsInitialized => _instance != null && _instance.isActiveAndEnabled && _instance._initialized;
        public static string RegistrationToken => IsInitialized ? _instance._token : null;
        public static bool HasRegistrationToken => IsInitialized && !string.IsNullOrEmpty(_instance._token);
        public static string TokenSuffix => HasRegistrationToken && _instance._token.Length > 6
            ? _instance._token.Substring(_instance._token.Length - 6)
            : HasRegistrationToken ? _instance._token : string.Empty;
        public static string LastError { get; private set; }
        public static event Action<string> OnTokenReceived;
        public static event Action<PushNotification> OnNotificationReceived;
        public static event Action<string> OnError;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            OnTokenReceived = null;
            OnNotificationReceived = null;
            OnError = null;
            LastError = null;
        }

        public void Initialize(bool requestPermissionOnStart = false)
        {
#if USE_FIREBASEMESSAGING
            if (_initialized || !isActiveAndEnabled) return;
            if (_instance != null && _instance != this) return;
            try
            {
                // Also disable native auto-init in platform configuration (see setup guide).
                FirebaseMessaging.TokenRegistrationOnInitEnabled = false;
                _instance = this;
                _initialized = true;
                _lifetime = new CancellationTokenSource();
                Subscribe();
                if (requestPermissionOnStart) _ = StartRegistrationAsync();
            }
            catch (Exception e)
            {
                OnDisable();
                _initialized = false;
                ReportError("FCM initialization failed: " + e.Message);
            }
#endif
        }

        /// <summary>Completes when the prompt is dismissed; completion does not imply authorization.</summary>
        public static Task RequestPermissionAsync()
        {
            if (!IsInitialized) return Task.FromException(Unavailable());
            var owner = _instance;
            if (owner._permissionTask == null || owner._permissionTask.IsCompleted)
                owner._permissionTask = owner.RequestPermissionInternalAsync(owner._generation);
            return owner._permissionTask;
        }

        /// <summary>Explicitly enables FCM registration and retrieves the current token.</summary>
        public static Task<string> GetTokenAsync()
        {
            if (!IsInitialized) return Task.FromException<string>(Unavailable());
            var owner = _instance;
            if (owner._tokenTask == null || owner._tokenTask.IsCompleted)
                owner._tokenTask = owner.GetTokenInternalAsync(owner._generation);
            return owner._tokenTask;
        }

        public static Task SubscribeToTopicAsync(string topic)
        {
            return ChangeTopicAsync(topic, true);
        }

        public static Task UnsubscribeFromTopicAsync(string topic)
        {
            return ChangeTopicAsync(topic, false);
        }

        private static Task ChangeTopicAsync(string topic, bool subscribe)
        {
            if (string.IsNullOrEmpty(topic) || !TopicPattern.IsMatch(topic))
                return Task.FromException(new ArgumentException("Use an unprefixed FCM topic with 1–900 letters, digits, or -_.~% characters.", nameof(topic)));
            if (!IsInitialized) return Task.FromException(Unavailable());
#if USE_FIREBASEMESSAGING
            return _instance.ChangeTopicInternalAsync(topic, subscribe, _instance._generation);
#else
            return Task.FromException(Unavailable());
#endif
        }

#if USE_FIREBASEMESSAGING
        private async Task ChangeTopicInternalAsync(string topic, bool subscribe, int generation)
        {
            var operation = subscribe ? FirebaseMessaging.SubscribeAsync(topic) : FirebaseMessaging.UnsubscribeAsync(topic);
            await WaitForOperationAsync(operation, generation, 30);
        }
#endif

        private async Task WaitForOperationAsync(Task operation, int generation, int timeoutSeconds)
        {
            EnsureCurrent(generation);
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token))
            {
                var wait = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), deadline.Token);
                if (await Task.WhenAny(operation, wait) != operation)
                {
                    // Native requests cannot be cancelled; observe any failure arriving after disposal/timeout.
                    _ = operation.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    EnsureCurrent(generation);
                    throw new TimeoutException("FCM operation timed out.");
                }
                deadline.Cancel();
                await operation;
                EnsureCurrent(generation);
            }
        }

        private async Task StartRegistrationAsync()
        {
            var generation = _generation;
            try
            {
                await RequestPermissionAsync();
                EnsureCurrent(generation);
                await GetTokenAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (IsCurrent(generation)) ReportError("FCM registration failed: " + e.Message);
            }
        }

        private async Task RequestPermissionInternalAsync(int generation)
        {
#if USE_FIREBASEMESSAGING
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                if (version.GetStatic<int>("SDK_INT") >= 33 &&
                    !UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
                {
                    var completion = new TaskCompletionSource<bool>();
                    _androidPermission = completion;
                    var callbacks = new UnityEngine.Android.PermissionCallbacks();
                    callbacks.PermissionGranted += _ => completion.TrySetResult(true);
                    callbacks.PermissionDenied += _ => completion.TrySetResult(false);
                    callbacks.PermissionDeniedAndDontAskAgain += _ => completion.TrySetResult(false);
                    UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS", callbacks);
                    await WaitForOperationAsync(completion.Task, generation, 120);
                    _androidPermission = null;
                }
            }
#else
            await WaitForOperationAsync(FirebaseMessaging.RequestPermissionAsync(), generation, 120);
#endif
            EnsureCurrent(generation);
#else
            await Task.FromException(Unavailable());
#endif
        }

        private async Task<string> GetTokenInternalAsync(int generation)
        {
#if USE_FIREBASEMESSAGING
            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;
            var operation = FirebaseMessaging.GetTokenAsync();
            await WaitForOperationAsync(operation, generation, 30);
            var token = operation.Result;
            PublishToken(token);
            return token;
#else
            return await Task.FromException<string>(Unavailable());
#endif
        }

        private void OnEnable()
        {
            if (!_initialized || (_instance != null && _instance != this)) return;
            _instance = this;
            _lifetime = new CancellationTokenSource();
            Subscribe();
        }

        private void Subscribe()
        {
#if USE_FIREBASEMESSAGING
            if (_subscribed) return;
            FirebaseMessaging.TokenReceived += HandleToken;
            FirebaseMessaging.MessageReceived += HandleMessage;
            _subscribed = true;
#endif
        }

#if USE_FIREBASEMESSAGING
        private void HandleToken(object sender, TokenReceivedEventArgs args)
        {
            if (IsCurrent(_generation)) PublishToken(args?.Token);
        }

        private void HandleMessage(object sender, MessageReceivedEventArgs args)
        {
            if (!IsCurrent(_generation) || args?.Message == null) return;
            var message = args.Message;
            var notification = new PushNotification(message.MessageId, message.From,
                message.Notification?.Title, message.Notification?.Body, message.Data);
            InvokeSafely(OnNotificationReceived, notification);
        }
#endif

        private void PublishToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token == _token) return;
            _token = token;
            LastError = null;
            InvokeSafely(OnTokenReceived, token);
        }

        private void OnDisable()
        {
            _generation++;
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
#if USE_FIREBASEMESSAGING
            if (_subscribed)
            {
                FirebaseMessaging.TokenReceived -= HandleToken;
                FirebaseMessaging.MessageReceived -= HandleMessage;
                _subscribed = false;
            }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR && USE_FIREBASEMESSAGING
            _androidPermission?.TrySetCanceled();
            _androidPermission = null;
#endif
            _tokenTask = null;
            _permissionTask = null;
            if (_instance == this) _instance = null;
        }

        private void OnDestroy()
        {
            OnDisable();
            _initialized = false;
            _token = null;
        }

        private bool IsCurrent(int generation) => IsInitialized && _instance == this && generation == _generation;
        private void EnsureCurrent(int generation)
        {
            if (!IsCurrent(generation)) throw new OperationCanceledException("FCM service stopped before the operation completed.");
        }

        private static InvalidOperationException Unavailable() =>
            new InvalidOperationException("FCM is not initialized. Enable Firebase Messaging and import FirebaseMessaging.unitypackage.");

        private static void ReportError(string message)
        {
            LastError = message;
            CustomDebug.LogWarning(message);
            InvokeSafely(OnError, message);
        }

        private static void InvokeSafely<T>(Action<T> callbacks, T value)
        {
            if (callbacks == null) return;
            foreach (Action<T> callback in callbacks.GetInvocationList())
            {
                try { callback(value); }
                catch (Exception e) { CustomDebug.LogError("FCM callback failed: " + e.GetType().Name); }
            }
        }
    }
}
