using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Owl
{
    public class TimerAdCanvasScript : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Text _timerText;
        [SerializeField] private UnityEngine.UI.Button _continueButton;
        [SerializeField] private UnityEngine.UI.Button _rewardMultiplyText;


        private void Awake()
        {
#if !UNITY_EDITOR
            var canvas = GetComponent<Canvas>();
            canvas.sortingOrder = 32760;
#endif
        }
        public void Init(System.Action OnContinue,System.Action OnRewardMultiply)
        {
            _continueButton?.onClick.AddListener(() =>
            {
                OnContinue?.Invoke();
            });
            
            _rewardMultiplyText?.onClick.AddListener(() =>
            {
                OnRewardMultiply?.Invoke();
            });
        }
        
        
        
        public void ShowTimerCanvas()
        {
            this.gameObject.SetActive(true);
        }
    
        public void HideTimerCanvas()
        {
            this.gameObject.SetActive(false);
        }
        
        public void UpdateTimerText(float time)
        {
            _timerText.text = "AD BREAK: " + (int) time;
        }
        
        

    }
}