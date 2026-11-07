using UnityEngine;

namespace Owl{
    
    public class AdComingLayerScript : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Text _timerText;
        [SerializeField] [Range(0.1f,2f)] private float _waitStep = 0.5f;
        private Coroutine _adBreakCol;
        private int _timer = 0;

        public void SetWaitStep(float waitStep)
        {
            _waitStep = Mathf.Clamp(waitStep, 0.1f, 2f);
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            var canvas = GetComponent<Canvas>();
            canvas.sortingOrder = 32767;
#endif
        }
        public void ShowAdBreakLayer(bool shouldShow,System.Action showAdAction)
        {
            if (shouldShow)
            {
                this.gameObject.SetActive(true);
                Monetization.AdManager.OnAdBreakLayerDisplayEvent?.Invoke(true);
                _adBreakCol ??= StartCoroutine(ShowAdBreakLayerDelay(showAdAction));
            }
            else
            {
                showAdAction?.Invoke();
            }
        }
        private System.Collections.IEnumerator ShowAdBreakLayerDelay(System.Action showAdAction)
        {
            _timer = 3;
            _timerText.text = _timer.ToString();
            while (_timer > 0)
            {
                yield return new WaitForSecondsRealtime(_waitStep);
                UpdateTimer();
            }
            _adBreakCol = null;
            DisableAdLayer();
            showAdAction?.Invoke();
        }
        public void DisableAdLayer()
        {
            _adBreakCol = null;
            this.gameObject.SetActive(false);
            Monetization.AdManager.OnAdBreakLayerDisplayEvent?.Invoke(false);
        }

        private void UpdateTimer()
        {
            _timer--;
            if (_timer<0)
                _timer = 0;
            
            _timerText.text = _timer.ToString();
        }
    }
}
