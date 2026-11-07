using UnityEngine;

namespace Owl.Monetization
{
    public class MRECPlacement : MonoBehaviour
    {
        public BannerPosition position;

        private void OnEnable()
        {
            Utility.InternetManager.OnOnlineCheckComplete += CheckAdLoad;
            AdManager.ShowMREC(position);
        }
        
        private void OnDisable()
        {
            AdManager.HideMREC();
            Utility.InternetManager.OnOnlineCheckComplete -= CheckAdLoad;
        }
        
        protected virtual void CheckAdLoad(bool result)
        {
            if (result)
                AdManager.ShowMREC(position);
        }
        
    }
}