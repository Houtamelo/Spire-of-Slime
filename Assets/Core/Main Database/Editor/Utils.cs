using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Main_Database.Editor
{
    public static class Utils
    {
        [Pure]
        public static bool TryFindAssetWithType<T>(out T obj) where T : Object
        {
            string typeName = typeof(T).Name;
            string filter = $"t:{typeName}";
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    obj = asset;
                    return true;
                }
            }

            obj = default;
            return false;
        }

        [Pure]
        public static List<T> FindAssetsByType<T>() where T : Object
        {
            List<T> assets = new List<T>();
            
            string typeName = typeof(T).Name;
            string filter = $"t:{typeName}";
            
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                    assets.Add(asset);
            }

            return assets;
        }

        [Pure]
        public static List<T> FindComponentsByType<T>() where T : MonoBehaviour
        {
            List<T> assets = new List<T>();
            
            //string typeName = typeof(T).Name;
            string filter = $"t:GameObject";
            
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (obj.TryGetComponent(out T asset))
                    assets.Add(asset);
            }

            return assets;
        }

        [Pure]
        public static List<T> FindAssetsByType<T>(string extension) where T : Object
        {
            List<T> assets = new List<T>();
            
            string typeName = typeof(T).Name;
            string filter = $"t:{typeName}";
            
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetExtension(assetPath) != extension)
                    continue;

                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                    assets.Add(asset);
            }

            return assets;
        }

        [Pure]
        public static List<T> GetAllAssetsOfTypeInFoldersWithName<T>(string folderName, string topMostFolder) where T : Object
        {
            string[] folders = AssetDatabase.GetSubFolders(topMostFolder);
            HashSet<string> folderPaths = new HashSet<string>();
            
            foreach (string folder in folders)
                RecursiveAddSubFolders(folder, ref folderPaths);
            
            folderPaths.RemoveWhere(f => Path.GetFileName(f) != folderName);

            string typeName = typeof(T).Name;
            string filter = $"t:{typeName}";

            List<T> assets = new List<T>();
            foreach (string folderPath in folderPaths)
            {
                string[] guids = AssetDatabase.FindAssets(filter: filter, new []{folderPath});
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    if (asset != null)
                        assets.Add(asset);
                }
            }
            
            return assets;
        }

        [Pure]
        public static List<T> GetAllAssetsOfTypeWhereNameStartsWith<T>(string pattern) where T : Object
        {
            List<T> assets = FindAssetsByType<T>();
            assets.RemoveAll(a => !a.name.StartsWith(pattern));
            return assets;
        }
        
        private static void RecursiveAddSubFolders(string folder, ref HashSet<string> folderPaths)
        {
            folderPaths.Add(folder);
            string[] folders = AssetDatabase.GetSubFolders(folder);
            foreach (string subFolder in folders)
                RecursiveAddSubFolders(subFolder, ref folderPaths);
        }
    }
}