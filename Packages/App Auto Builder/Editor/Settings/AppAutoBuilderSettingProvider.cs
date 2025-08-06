using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEditor.Presets;

namespace zFramework.AppBuilder
{
    /// <summary>
    /// Settings provider for the AppAutoBuilder settings 
    /// (accessible from Tools > App Auto Builder)
    /// Saves the settings in a json file in the ProjectSettings folder.
    /// </summary>
    public partial class AppAutoBuilderSettingProvider : SettingsProvider
    {
        const string aboutMessage = @"Unity 一键打包工具，适用于 频繁切换打包场景的情景，一劳永逸，一键打多个app ；
Unity one-click packaging tool, suitable for scenarios where the packaging scene is frequently switched. One-time effort to package multiple apps with just one click.";
        const string repositoryLink = "https://github.com/Bian-Sh/Unity-Application-Auto-Builder";

        static private AppAutoBuilderSettings settings;
        static SerializedObject serializedObject;

        private const string PATH = "Project/App Auto Builder";
        private const string TITLE = "App Auto Builder";
        const string SettingsPath = "ProjectSettings/AppAutoBuilderSettings.asset";
        string version = "1.0.0";

        public AppAutoBuilderSettingProvider() : base(PATH, SettingsScope.Project)
        {
            label = TITLE;
        }

        override public void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            serializedObject = new SerializedObject(Settings);
            var path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(settings));
            var jsonfile = Path.Combine(Path.GetDirectoryName(path), "..\\..", "package.json");
            if (File.Exists(jsonfile))
            {
                var json = File.ReadAllLines(jsonfile);
                //   "version": "1.3.0",
                foreach (var line in json)
                {
                    var str = line.Trim();
                    if (str.StartsWith("\"version\":"))
                    {
                        var arr = str.Split("\"", System.StringSplitOptions.RemoveEmptyEntries);
                        version = arr[2];
                        break;
                    }
                }
            }
        }

        public override void OnTitleBarGUI()
        {
            //draw a preset button here on the right title bar
            var icon = EditorGUIUtility.IconContent("d_Preset.Context");
            var style = GUI.skin.GetStyle("IconButton");
            var content = new GUIContent(icon.image, "Load or saving preset");
            if (GUILayout.Button(content, style))
            {
                var receiver = ScriptableObject.CreateInstance<PresetReceiver>();
                receiver.Init(settings, this);
                PresetSelector.ShowSelector(settings, null, true, receiver);
            }
        }

        /// <summary>
        /// GUI callback for drawing the settings provider GUI.
        /// </summary>
        /// <param name="searchContext">The search context for the GUI.</param>
        public override void OnGUI(string searchContext)
        {
            GUILayout.Space(10);
            if (serializedObject?.targetObject == null)
            {
                serializedObject = new SerializedObject(Settings);
            }
            // draw the settings GUI
            using var changescope = new EditorGUI.ChangeCheckScope();
            serializedObject.Update();

            var virboxExePath = serializedObject.FindProperty("virboxExePath");
            var virboxLMExePath = serializedObject.FindProperty("virboxLMExePath");
            var devloperId = serializedObject.FindProperty("devloperId");
            var virboxPsw = serializedObject.FindProperty("virboxPsw");
            var virboxApiPsw = serializedObject.FindProperty("virboxApiPsw");

            var nsisExePath = serializedObject.FindProperty("nsisExePath");
            var shouldKeepNsisFile = serializedObject.FindProperty("shouldKeepNsisFile");

            FilePathWithOpenDialog(virboxExePath, "请选择 Virbox 命令行程序");
            FilePathWithOpenDialog(virboxLMExePath, "请选择 Virbox (LM )命令行程序");

            // 如果 lm path 有效，则显示 devloperId 和 virboxPsw
            if (!string.IsNullOrEmpty(virboxLMExePath.stringValue))
            {
                using var box = new EditorGUILayout.VerticalScope(GUI.skin.box);
                EditorGUILayout.PropertyField(devloperId);
                settings.VirboxPin = EditorGUILayout.PasswordField("Virbox Password", settings.VirboxPin);
                EditorGUILayout.PropertyField (virboxApiPsw);
            }

            // NSIS
            FilePathWithOpenDialog(nsisExePath, "请选择 NSIS 命令行程序");

            //  toggle right here for shouldKeepNsisFile
            EditorGUILayout.PropertyField(shouldKeepNsisFile);

            if (changescope.changed)
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(Settings, "App Auto Builder Settings");
                SaveSettings();
            }

            // ABOUT
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(aboutMessage, EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("More info: " + repositoryLink, EditorStyles.linkLabel)) Application.OpenURL(repositoryLink);
            GUILayout.EndVertical();
        }

        private void FilePathWithOpenDialog(SerializedProperty property, string title)
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property);
                // button for open file dialog
                if (GUILayout.Button(openfiledilog, GUILayout.Width(30)))
                {
                    var path = EditorUtility.OpenFilePanel(title, "", "exe");
                    if (!string.IsNullOrEmpty(path))
                    {
                        property.stringValue = path;
                    }
                }
            }
        }

        public override void OnFooterBarGUI()
        {
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 12,
                wordWrap = true
            };
            GUILayout.Label($"Version {version}", style);
        }

        /// <summary>
        /// Loads the settings from the settings file or creates new default settings if the file doesn't exist.
        /// </summary>
        private static void LoadSettings()
        {
            string dir = Path.GetDirectoryName(SettingsPath);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(SettingsPath))
            {
                var arr = InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
                if (arr.Length > 0)
                {
                    settings = (AppAutoBuilderSettings)arr[0];
                }
                if (settings == null)
                {
                    throw new InvalidDataException($"There was an error in the settings file,check this file : {SettingsPath}!");
                }
            }
            else
            {
                settings = ScriptableObject.CreateInstance<AppAutoBuilderSettings>();
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { settings }, SettingsPath, true);
            }
        }

        /// <summary>
        /// Saves the settings to the settings file.
        /// </summary>
        public void SaveSettings() => InternalEditorUtility.SaveToSerializedFileAndForget(new[] { settings }, SettingsPath, true);

        /// <summary>
        /// Gets the ExportProjectToZip settings (loads them if they are not loaded yet).
        /// </summary>
        public static AppAutoBuilderSettings Settings
        {
            get
            {
                if (settings == null) LoadSettings();
                return settings;
            }
        }

        /// <summary>
        /// Creates the settings provider instance.
        /// </summary>
        /// <returns>The newly created SettingsProvider instance.</returns>
        [SettingsProvider]
        static public SettingsProvider CreateSettingsProvider()
        {
            return new AppAutoBuilderSettingProvider();
        }

        internal static void ShowSettings()
        {
            SettingsService.OpenProjectSettings(PATH);
        }

        readonly GUIContent openfiledilog = new("···", "Open File Dialog");

    }
}
