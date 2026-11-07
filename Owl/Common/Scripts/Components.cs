using UnityEngine;

namespace Owl
{
    public class Components : ScriptableObject
    {
        [Space]
        public Monetization.MRECPlacement MrecPlacement;
        public OwlStatusScript OwlStatusCanvas;
        public AdComingLayerScript AdComingLayerCanvas;
        public TimerAdCanvasScript TimerAdCanvas;


        [Space]
        public GameObject NoInternetCanvas;
        
        
        [Space]
        public GameObject IapLoadingCanvas;

        
        public BannerBgLayerScript BannerBgLayerCanvasP,BannerBgLayerCanvasL;
        public MRECBgLayerScript MrecBgLayerCanvasP,MrecBgLayerCanvasL;
        
        
        #region UtilityCalls
        public AdComingLayerScript SpawnAdComingLayerCanvas(float waitStep = 0.5f)
        {
            var canvas=Instantiate(AdComingLayerCanvas, OwlManager.GetCanvasesParent());
            canvas?.SetWaitStep(waitStep);
            canvas?.gameObject.SetActive(false);
            return canvas;
        }
        
        public OwlStatusScript SpawnOwlStatusCanvas()
        {
            var canvas=Instantiate(OwlStatusCanvas, OwlManager.GetCanvasesParent());
            return canvas;
        }
        
        public BannerBgLayerScript SpawnBannerBgLayerCanvas()
        {
            var isPortrait = Screen.height > Screen.width;
            var bannerBg = isPortrait ? BannerBgLayerCanvasP : BannerBgLayerCanvasL;
            var canvas=Instantiate(bannerBg, OwlManager.GetCanvasesParent());
            return canvas;

        }
        public MRECBgLayerScript SpawnMrecBgLayerCanvas()
        {
            var isPortrait = Screen.height > Screen.width;
            var bannerBg = isPortrait ? MrecBgLayerCanvasP : MrecBgLayerCanvasL;
            var canvas=Instantiate(bannerBg, OwlManager.GetCanvasesParent());
            return canvas;
        }
        
        public TimerAdCanvasScript SpawnTimerAdCanvas(Transform parentT)
        {
            var canvas=Instantiate(TimerAdCanvas, OwlManager.GetCanvasesParent());
            return canvas;
        }


        #endregion


    }
}
