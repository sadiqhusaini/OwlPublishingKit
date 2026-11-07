using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Owl
{
    [InitializeOnLoad]
    public static class PrefabIconAssigner
    {
        private static OwlSettings _owlSettings;

        [Obsolete("Obsolete")]
        static PrefabIconAssigner()
        {
            EditorApplication.delayCall += AssignIcons;
        }

        [Obsolete("Obsolete")]
        private static void AssignIcons()
        {
            _owlSettings ??= Resources.Load<OwlSettings>("OwlSettings");
            var icon = _owlSettings.IconTexture;
       

            if (icon == null) return;

            foreach (var go in Object.FindObjectsOfType<OwlManager>())
            {
                if (go)
                {
                    EditorGUIUtility.SetIconForObject(go, icon);    
                }
            }
        }

   
    }

}