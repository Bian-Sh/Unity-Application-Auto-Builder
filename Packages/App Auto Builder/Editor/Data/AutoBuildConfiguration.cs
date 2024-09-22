using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace zFramework.AppBuilder
{
    public class AutoBuildConfiguration : ScriptableObject
    {
        [OpenFolder, Header("保存路径(Root)")]
        public string appLocationPath;
        [Header("软件出包配置")]
        public List<BuildProfiles> profiles;

        public static AutoBuildConfiguration LoadOrCreate()
        {
            var file = "Assets/AutoBuilder/AutoBuildConfiguration.asset";
            var config = AssetDatabase.LoadAssetAtPath<AutoBuildConfiguration>(file);
            if (!config)
            {
                config = CreateInstance<AutoBuildConfiguration>();
                var dir = new DirectoryInfo(file);
                if (!dir.Parent.Exists)
                {
                    dir.Parent.Create();
                }
                AssetDatabase.CreateAsset(config, file);
                AssetDatabase.SaveAssets();
            }
            return config;
        }
    }
}