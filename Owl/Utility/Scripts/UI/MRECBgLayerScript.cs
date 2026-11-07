using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Owl
{
    public class MRECBgLayerScript : MonoBehaviour
    {
        public List<GameObject> allBanners=new List<GameObject>();

        public void EnableMRECBg(BannerPosition _pos)
        {
            DisableMRECBg();

            foreach (var t in allBanners.Where(t => _pos.ToString()==t.name))
            {
                t.SetActive(true);
                break;
            }
        }

        public void DisableMRECBg()
        {
            foreach (var t in allBanners)
            {
                t.SetActive(false);
            }
        }
    }}