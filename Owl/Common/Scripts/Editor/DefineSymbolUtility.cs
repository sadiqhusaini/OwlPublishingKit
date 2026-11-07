using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Owl.Common
{
    [DefaultExecutionOrder(-10000000)]
    public class DefineSymbolUtility : MonoBehaviour
    {
        private static readonly BuildTargetGroup[] TargetGroups = 
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS
        };
        
        
        [Obsolete("Obsolete")]
        public static void ScriptingDefineSymbol(string symbol, bool add)
        {
            if(!add)
               RemoveScriptingDefineSymbol(symbol);
            else
               AddScriptingDefineSymbol(symbol);
        }

        [Obsolete("Obsolete")]
        public static void AddScriptingDefineSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;

            foreach (var group in TargetGroups)
            {
                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                if (!defines.Contains(symbol))
                {
                    defines = string.IsNullOrEmpty(defines) ? symbol : $"{defines};{symbol}";
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
                    CustomDebug.Log($"Added scripting define symbol '{symbol}' for {group}");
                }
            }
        }

        [Obsolete("Obsolete")]
        public static void RemoveScriptingDefineSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;

            foreach (var group in TargetGroups)
            {
                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                if (defines.Contains(symbol))
                {
                    var defineList = new List<string>(defines.Split(';'));
                    defineList.RemoveAll(s => s == symbol);
                    string newDefines = string.Join(";", defineList);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefines);
                    CustomDebug.Log($"Removed scripting define symbol '{symbol}' for {group}");
                }
            }
        }
        
       
    }
}