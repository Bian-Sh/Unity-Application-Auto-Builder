using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace zFramework.Extension
{
    public class AutoBuildConfiguration : ScriptableObject
    {
        [Header("打包平台，选不中的就别纠结")]
        public BuildTarget targetPlatform = BuildTarget.StandaloneWindows;
        [OpenFolder, Header("打包保存路径")]
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