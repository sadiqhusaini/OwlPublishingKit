using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Owl.Common
{
    public static class ScopedRegistryUtils
    {
        private const string ManifestPath = "Packages/manifest.json";
        private const string RegistryKey = "\"scopedRegistries\"";

        [Serializable]
        private class RegistryManifest
        {
            public ScopedRegistry[] scopedRegistries;
        }

        [Serializable]
        private class ScopedRegistry
        {
            public string name;
            public string url;
            public string[] scopes;
        }

        public static void AddScopedRegistry(string name, string url, string[] scopes)
        {
            if (!TryLoad(out var manifest, out var registries)) return;
            if (registries.Any(registry => registry.name == name && registry.url == url &&
                                           (registry.scopes ?? Array.Empty<string>()).SequenceEqual(scopes)))
            {
                CustomDebug.Log($"Scoped registry already exists: {name}");
                return;
            }

            registries.Add(new ScopedRegistry { name = name, url = url, scopes = scopes });
            SaveManifest(manifest, registries);
            CustomDebug.Log($"Added scoped registry: {name}");
        }

        public static void RemoveScopedRegistry(string name)
        {
            if (!TryLoad(out var manifest, out var registries)) return;
            if (registries.RemoveAll(registry => registry.name == name) == 0)
            {
                CustomDebug.LogWarning($"No scoped registry found with name: {name}");
                return;
            }

            SaveManifest(manifest, registries);
            CustomDebug.Log($"Removed scoped registry: {name}");
        }

        public static void RemoveScopedRegistryByUrl(string url)
        {
            if (!TryLoad(out var manifest, out var registries)) return;
            if (registries.RemoveAll(registry => registry.url == url) == 0)
            {
                CustomDebug.LogWarning($"No scoped registry found with URL: {url}");
                return;
            }

            SaveManifest(manifest, registries);
            CustomDebug.Log($"Removed scoped registry with URL: {url}");
        }

        public static void RemoveScopes(string registryName, string[] scopesToRemove)
        {
            if (!TryLoad(out var manifest, out var registries)) return;
            var registry = registries.FirstOrDefault(item => item.name == registryName);
            if (registry == null)
            {
                CustomDebug.LogWarning($"Registry not found: {registryName}");
                return;
            }

            registry.scopes = (registry.scopes ?? Array.Empty<string>())
                .Where(scope => !scopesToRemove.Contains(scope)).ToArray();
            SaveManifest(manifest, registries);
            CustomDebug.Log($"Removed scopes from registry '{registryName}': {string.Join(", ", scopesToRemove)}");
        }

        private static bool TryLoad(out string manifest, out List<ScopedRegistry> registries)
        {
            manifest = null;
            registries = null;
            if (!File.Exists(ManifestPath))
            {
                CustomDebug.LogError("manifest.json not found!");
                return false;
            }

            try
            {
                manifest = File.ReadAllText(ManifestPath);
                var data = JsonUtility.FromJson<RegistryManifest>(manifest);
                registries = new List<ScopedRegistry>(data?.scopedRegistries ?? Array.Empty<ScopedRegistry>());
                return true;
            }
            catch (Exception exception)
            {
                CustomDebug.LogError($"Could not read manifest.json: {exception.Message}");
                return false;
            }
        }

        private static void SaveManifest(string manifest, List<ScopedRegistry> registries)
        {
            var registryJson = JsonUtility.ToJson(new RegistryManifest { scopedRegistries = registries.ToArray() }, true);
            var arrayStart = registryJson.IndexOf('[');
            var arrayEnd = registryJson.LastIndexOf(']');
            var arrayJson = registryJson.Substring(arrayStart, arrayEnd - arrayStart + 1);

            if (TryFindRegistryArray(manifest, out var start, out var length))
            {
                manifest = manifest.Remove(start, length).Insert(start, arrayJson);
            }
            else
            {
                var closingBrace = manifest.LastIndexOf('}');
                if (closingBrace < 0)
                    throw new InvalidDataException("manifest.json has no closing object brace.");

                var beforeBrace = manifest.Substring(0, closingBrace).TrimEnd();
                var separator = beforeBrace.EndsWith("{", StringComparison.Ordinal) ? "" : ",";
                manifest = manifest.Insert(closingBrace, separator + "\n  " + RegistryKey + ": " + arrayJson + "\n");
            }

            File.WriteAllText(ManifestPath, manifest);
            AssetDatabase.Refresh();
        }

        private static bool TryFindRegistryArray(string manifest, out int start, out int length)
        {
            start = -1;
            length = 0;
            var key = manifest.IndexOf(RegistryKey, StringComparison.Ordinal);
            if (key < 0) return false;

            var colon = manifest.IndexOf(':', key + RegistryKey.Length);
            if (colon < 0) throw new InvalidDataException("scopedRegistries has no value.");
            start = manifest.IndexOf('[', colon + 1);
            if (start < 0) throw new InvalidDataException("scopedRegistries is not an array.");

            var depth = 0;
            var inString = false;
            var escaped = false;
            for (var index = start; index < manifest.Length; index++)
            {
                var character = manifest[index];
                if (escaped) { escaped = false; continue; }
                if (inString && character == '\\') { escaped = true; continue; }
                if (character == '"') { inString = !inString; continue; }
                if (inString) continue;
                if (character == '[') depth++;
                if (character != ']' || --depth != 0) continue;
                length = index - start + 1;
                return true;
            }

            throw new InvalidDataException("scopedRegistries array is incomplete.");
        }
    }
}
