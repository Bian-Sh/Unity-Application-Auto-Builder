using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
namespace zFramework.Extension
{
    public class AutoBuilder : EditorWindow
    {
        [MenuItem("Tools/App Auto Builder")]
        public static void Init()
        {
            var window = GetWindow(typeof(AutoBuilder));
            window.titleContent = new GUIContent("App Auto Builder");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        void OnEnable()
        {
            config = AutoBuildConfiguration.LoadOrCreate();
            serializedObject = new SerializedObject(config);
            InitProperties();
        }
        void InitProperties()
        {
            targetPlatform = serializedObject.FindProperty("targetPlatform");
            appLocationPath = serializedObject.FindProperty("appLocationPath");
            profiles = serializedObject.FindProperty("profiles");
        }
        void OnGUI()
        {
            serializedObject.Update();
            InitProperties();
            using (new GUILayout.VerticalScope())
            {
                EditorGUILayout.PropertyField(targetPlatform);
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
                        ShowNotification(op_content);
                        GUIUtility.ExitGUI();
                    }
                    GUI.color = color;
                    GUILayout.FlexibleSpace();
                }
            }
            serializedObject.ApplyModifiedProperties();
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
        }
        static void BuildPlayer(AutoBuildConfiguration config)
        {
            ValidateProfiles(config);
            foreach (var profile in config.profiles)
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
                            item.Run();
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
                    var dir = $"{config.appLocationPath}/{profile.productName}";
                    var ext = config.targetPlatform switch
                    {
                        BuildTarget.StandaloneWindows => ".exe",
                        BuildTarget.StandaloneWindows64 => ".exe",
                        BuildTarget.Android => ".apk",
                        _ => throw new Exception("不支持的打包平台！") // TODO: 其他平台的后缀请各领域专家补充，欢迎提 PR
                    };
                    var file = $"{dir}/{profile.productName}{ext}";
                    PlayerSettings.productName = profile.productName;
                    PlayerSettings.bundleVersion = profile.productVersion;
                    var report = BuildPipeline.BuildPlayer(scenes.ToArray(), file, config.targetPlatform, options_unity);
                    Debug.Log($"{profile.productName} 打包结果：{report.summary.result}");
                }
            }
            Debug.Log($" 打包结束，可通过控制台确认所有打包结果");
        }

        #region Callbacks 
        [PostProcessBuild]
        static void OnPostProcessBuild(BuildTarget target, string output)
        {
            config ??= AutoBuildConfiguration.LoadOrCreate();
            var productname = Path.GetFileNameWithoutExtension(output);
            var profile = config.profiles.FirstOrDefault(v => v.productName == productname);
            if (null != profile)
            {
                var tasks = profile.customTask.Where(v => v.enabled)
                    .Select(v => v.task)
                    .Where(v => v.taskType == TaskType.PostBuild)
                     .OrderBy(v => v.priority);
                foreach (var item in tasks)
                {
                    if (item)
                    {
                        item.Run();
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
        }
        #endregion

        #region Private Fields
        static AutoBuildConfiguration config;
        GUIContent build_content = new GUIContent("打包", "点击将按上述配置依次进行打包！");
        GUIContent op_content = new GUIContent("打包结束，请确认是否打包成功！");
        SerializedObject serializedObject;
        SerializedProperty targetPlatform;
        SerializedProperty appLocationPath;
        SerializedProperty profiles;
        Vector2 pos = Vector2.zero;
        #endregion
    }
}