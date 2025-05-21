using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scripts.Editor
{
    public class EditorUtility
    {
        public static void GetAllPaths(GameObject prefab, List<string> paths)
        {
            if (paths == null)
            {
                throw new System.ArgumentException($"Provided {nameof(paths)} is null.");
            }

            paths.Clear();
            CollectInnerPaths(prefab.transform, "", paths);
        }

        public static List<string> GetAllPaths(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"Provided {nameof(prefab)} is null.");
                return new List<string>();
            }

            List<string> paths = new List<string>();
            CollectInnerPaths(prefab.transform, "", paths);
            return paths;
        }

        private static void CollectInnerPaths(Transform current, string parentPath, List<string> paths)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? current.name : $"{parentPath}/{current.name}";
            paths.Add(currentPath);

            foreach (Transform child in current)
            {
                CollectInnerPaths(child, currentPath, paths);
            }
        }

        private static GameObject[] GetPrefabAssetsWithName(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
            if (guids.Length > 0)
            {
                return guids.Select(g => 
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }).ToArray();
            }

            return null;
        }

        public bool IsSubPath(string parentPath, string[] subPaths) 
        {
            foreach (string path in subPaths) 
            {
                if (path == parentPath) 
                    return true; 

                if (parentPath.StartsWith(path) && 
                    parentPath.Length > path.Length && 
                    parentPath[path.Length] == '/') 
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}