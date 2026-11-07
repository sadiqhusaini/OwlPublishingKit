using UnityEngine;

namespace Owl.Monetization
{
    public class IAPRestoreButtonScript : MonoBehaviour
    {
        private void Start()
        {
            if (Application.platform==RuntimePlatform.Android)
            {
                gameObject.SetActive(false);
            }
            
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnRestoreClick);
        }

        private void OnRestoreClick()
        {
            IAPManager.OnRestore();
        }
    }
}