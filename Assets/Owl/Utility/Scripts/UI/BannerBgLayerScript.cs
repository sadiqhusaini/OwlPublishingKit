using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Owl
{
    public class BannerBgLayerScript : MonoBehaviour
    {
        public List<GameObject> allBanners=new List<GameObject>();


        public void EnableBannerBg(BannerPosition _pos)
        {
            DisableBannerBg();

            foreach (var t in allBanners.Where(t => _pos.ToString()==t.name))
            {
                t.SetActive(true);
                break;
            }

        }

        public void DisableBannerBg()
        {
            foreach (var t in allBanners)
            {
                t.SetActive(false);
            }
        }
    }
}