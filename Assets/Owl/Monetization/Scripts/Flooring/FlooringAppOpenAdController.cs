using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Owl.Monetization
{
    public class FlooringAppOpenAdController : MonoBehaviour
    {
        public List<AppOpenAdController> appOpenAdControllers= new List<AppOpenAdController>();

        private AppOpenAdController _appOpenAdController;
        
        public void AddAppOpenAdController(AppOpenAdController appOpenAdController)
        {
            appOpenAdControllers.Add(appOpenAdController);
        }
        
        public void ShowAppOpen(string placement)
        {
            if (IsAppOpenAvailable())
            {
                _appOpenAdController.ShowAd(placement);
            }
        }
        
        public bool IsAppOpenAvailable()
        {
            foreach (var t in appOpenAdControllers.Where(t => t.IsAppOpenAvailable()))
            {
                _appOpenAdController = t;
                return true;
            }
            return false;
        }
        
        
    }
}
