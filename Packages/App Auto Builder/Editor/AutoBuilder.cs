using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace zFramework.AppBuilder
{
    public class AutoBuilder : EditorWindow
    {
        static EditorWindow window;
        [MenuItem("Tools/App Auto Builder")]
        public static void Init()
        {
            //获取插件版本
            var stack = new System.Diagnostics.StackTrace(true);
            var path = stack.GetFrame(0).GetFileName();
            var root = Path.GetDirectoryName(path);
            root = root.Substring(0, root.LastIndexOf("\\Editor", StringComparison.Ordinal));
            var content = File.ReadAllText($"{root}/package.json");
            var version = JsonUtility.FromJson<Version>(content);

            window = GetWindow(typeof(AutoBuilder));
            window.titleContent = new GUIContent($"App Auto Builder (v{version.version})");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        void OnEnable()
        {
            assetsDirInfo = new DirectoryInfo(Application.dataPath);
            config = AutoBuildConfiguration.LoadOrCreate();
            serializedObject = new SerializedObject(config);
            InitProperties();
        }
        void InitProperties()
        {
            appLocationPath = serializedObject.FindProperty("appLocationPath");
            profiles = serializedObject.FindProperty("profiles");
        }
        void OnGUI()
        {
            serializedObject.Update();
            InitProperties();
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    EditorGUILayout.PropertyField(appLocationPath);
                    GUILayout.Space(8);
                    using (var scroll = new GUILayout.ScrollViewScope(pos))
                    {
                        EditorGUILayout.PropertyField(profiles);
                        pos = scroll.scrollPosition;
                    }
                    EditorGUILayout.Space();
                    using (var hr = new EditorGUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.FlexibleSpace();
                        var color = GUI.color;
                        GUI.color = new Color32(127, 214, 253, 255);
                        if (GUILayout.Button(build_content, GUILayout.Height(36), GUILayout.Width(120)))
                        {
                            BuildPlayer(config);
                            GUIUtility.ExitGUI();
                        }
                        GUI.color = color;
                        GUILayout.FlexibleSpace();
                    }
                }
                if (scope.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }


        /// <summary>
        /// 配置校验，如果配置不对不打包
        /// </summary>
        /// <param name="config"></param>
        static void ValidateProfiles(AutoBuildConfiguration config)
        {
            if (string.IsNullOrEmpty(config.appLocationPath))
            {
                throw new Exception("应用打包存储路径未指定！");
            }
            if (config.profiles.Any(v => string.IsNullOrEmpty(v.productName)))
            {
                throw new Exception("配置中存在软件名称为空的情况，请修复！");
            }
            if (config.profiles.Any(v => v.platform == Platform.None))
            {
                throw new Exception("配置中存在打包平台未指定的情况，请修复！");
            }
            if (config.profiles.All(v => !v.isBuild))
            {
                throw new Exception("请至少保证一个打包的配置 isBuild = true");
            }

            foreach (var profile in config.profiles)
            {
                if (profile.isBuild)
                {
                    if (profile.scenes.Count == 0)
                    {
                        throw new Exception($"请至少给 {profile.productName} 一个场景！");
                    }
                    if (profile.scenes.Any(v => !v.scene))
                    {
                        throw new Exception("场景列表请不要留空！");
                    }
                    if (profile.scenes.All(v => !v.enabled))
                    {
                        throw new Exception($"{profile.productName} 场景列表请至少勾选一个场景！");
                    }
                }
            }
            // 不可以存在相同的 Profile , 约定：ProductName 、 Platform 以及 sublocation 完全相同代表重复
            var groups = config.profiles.GroupBy(v => new { v.productName, v.platform, v.saveLocation });
            foreach (var group in groups)
            {
                if (group.Count() > 1)
                {
                    throw new Exception($"{group.Key.productName} 配置重复，至少 SubLocation 要不同！");
                }
            }
        }

        static void BuildPlayer(AutoBuildConfiguration config)
        {
            ValidateProfiles(config);
            var profiles = SortProfiles(config);

            foreach (var profile in profiles)
            {
                if (profile.isBuild)
                {
                    var tasks = profile.customTask.Where(v => v.enabled)
                        .Select(v => v.task)
                        .Where(v => v.taskType == TaskType.PreBuild)
                         .OrderBy(v => v.priority);
                    foreach (var item in tasks)
                    {
                        if (item)
                        {
                            Debug.Log($"执行 <color=green>{item.name}</color> PreBuild 任务");
                            item.RunAsync(string.Empty); // 打包前任务不需要参数
                        }
                        else
                        {
                            Debug.LogError($"{nameof(AutoBuilder)}: Custom Task is Missing!");
                        }
                    }

                    var scenes = new List<EditorBuildSettingsScene>();
                    foreach (var scene in profile.scenes)
                    {
                        if (scene.enabled)
                        {
                            var path = AssetDatabase.GetAssetPath(scene.scene);
                            scenes.Add(new EditorBuildSettingsScene(path, true));
                        }
                    }
                    var options = Enum.GetNames(typeof(BuildOptionsLit));
                    var options_unity = BuildOptions.None;
                    foreach (var item in options)
                    {
                        var op = (BuildOptionsLit)Enum.Parse(typeof(BuildOptionsLit), item);
                        if (profile.buildOptions.HasFlag(op))
                        {
                            var op_unity = (BuildOptions)Enum.Parse(typeof(BuildOptions), item);
                            options_unity |= op_unity;
                        }
                    }
                    var location = FormatPathToFullName(config.appLocationPath);
                    var dir = Path.Combine(location, profile.saveLocation);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var buildTarget = (BuildTarget)(int)profile.platform;

                    var ext = buildTarget switch
                    {
                        BuildTarget.StandaloneWindows => ".exe",
                        BuildTarget.StandaloneWindows64 => ".exe",
                        BuildTarget.Android => ".apk",
                        _ => string.Empty
                    };
                    var file = $"{dir}/{profile.productName}{ext}";
                    PlayerSettings.productName = profile.productName;
                    PlayerSettings.bundleVersion = profile.productVersion;
                    var report = BuildPipeline.BuildPlayer(scenes.ToArray(), file, buildTarget, options_unity);
                    Debug.Log($"{profile.productName} 打包结果：{report.summary.result}");
                }
            }
        }

        #region Assitant Fucntions
        /// <summary>
        /// 按 activeBuildTarget 最先打包，其他的按 platform 非乱序打包
        /// </summary>
        private static List<BuildProfiles> SortProfiles(AutoBuildConfiguration config)
        {
            var list = new List<BuildProfiles>();
            // 抽离出需要打包的配置
            var profiles = config.profiles.Where(v => v.isBuild).ToList();
            // 找到当前 activebuildtarget 对应的所有配置
            var actived_profiles = profiles.FindAll(v => v.platform == (Platform)(int)EditorUserBuildSettings.activeBuildTarget);
            if (actived_profiles != null)
            {
                list.AddRange(actived_profiles);
            }
            // 对其他配置进行排序，保证不乱序即可
            var other_profiles = profiles.FindAll(v => v.platform != (Platform)(int)EditorUserBuildSettings.activeBuildTarget);
            if (other_profiles != null)
            {
                other_profiles.Sort((a, b) => b.platform.CompareTo(a.platform));
                list.AddRange(other_profiles);
            }
            return list;
        }
        private static string FormatPathToFullName(string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                return $"{assetsDirInfo.Parent.FullName}/{fallbackPath}";
            }
            if (stringValue.StartsWith("./"))
            {
                stringValue = stringValue.Replace("./", $"{assetsDirInfo.Parent.FullName}/");
            }
            return stringValue;
        }
        #endregion

        #region Callbacks 
        [PostProcessBuild]
        async static void OnPostProcessBuild(BuildTarget target, string output)
        {
            config ??= AutoBuildConfiguration.LoadOrCreate();
            var productname = Path.GetFileNameWithoutExtension(output);
            var profile = config.profiles.FirstOrDefault(v => v.productName == productname && v.platform == (Platform)target && v.isBuild);
            if (null != profile)
            {
                var tasks = profile.customTask.Where(v => v.enabled && v.task.taskType == TaskType.PostBuild)
                    .Select(v => v.task)
                     .OrderBy(v => v.priority);
                var param = output;
                foreach (var item in tasks)
                {
                    if (item)
                    {
                        Debug.Log($"执行<color=green>{item.name}</color> PostBuild 任务");
                        param = await item.RunAsync(param);
                    }
                    else
                    {
                        Debug.LogError($"{nameof(AutoBuilder)}: Custom Task is Missing!");
                    }
                }
            }
            else
            {
                Debug.LogError($"{nameof(AutoBuilder)}:  找不到 {output} 的配置\nproductName = {productname}");
            }
            // 打包结束 window 实例会被销毁，所以这里需要重新获取
            window = GetWindow(typeof(AutoBuilder));
            window.ShowNotification(new GUIContent("打包结束，请确认是否打包成功！"));
            Debug.Log($" 打包结束，可通过控制台确认所有打包结果");
        }
        #endregion

        #region Private Fields
        static AutoBuildConfiguration config;
        GUIContent build_content = new GUIContent("打包", "点击将按上述配置依次进行打包！");
        SerializedObject serializedObject;
        SerializedProperty appLocationPath;
        SerializedProperty profiles;
        Vector2 pos = Vector2.zero;
        public const string fallbackPath = "Build";
        public static DirectoryInfo assetsDirInfo;
        #endregion

        #region Assistance Type
        [Serializable]
        public class Version
        {
            public string version;
        }
        #endregion
    }
}