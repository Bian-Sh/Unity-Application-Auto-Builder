using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace zFramework.AppBuilder
{
    public class OpenFolderAttribute : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(OpenFolderAttribute))]
    public class OpenFolderAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _lable)
        {
            var rect = new Rect(_position);
            rect.width -= 60;
            _property.stringValue = EditorGUI.TextField(rect, _property.displayName, _property.stringValue);
            var rect_bt = new Rect(_position);
            rect_bt.x = rect_bt.width - (EditorGUIUtility.hierarchyMode ? 44 : 54);
            rect_bt.width = 58;
            if (GUI.Button(rect_bt, content))
            {
                var defaultpath = string.IsNullOrEmpty(_property.stringValue) ? $"{AutoBuilder.assetsDirInfo.Parent.FullName}/{AutoBuilder.fallbackPath}" : _property.stringValue;
                var path = EditorUtility.OpenFolderPanel("请选择路径", defaultpath, string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    GUIUtility.keyboardControl = 0;
                    _property.stringValue = FormatPathToRelative(path);
                }
            }
        }

        // 默认路径是当前工程目录内的 Build 文件夹
        // 如果用户选择的是当前工程的 Assets 文件夹，就报错并设置为上一次的值,如果上一次的值无意义，就设置为约定的默认值
        // 如果用户选择的路径是当前工程内的文件夹，就将其转换成相对路径，否则就是绝对路径
        // 序列化的是相对路径，这样方便多人合作.
        // 假设当前工程目录是 E:/UnityProjects/OneApp
        // ./Build  =>  E:/UnityProjects/OneApp/Build
        // E:/UnityProjects/Output/OneApp => 不是当前项目子目录原样输出
        private string FormatPathToRelative(string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                Debug.LogWarning($"AutoBuilder: 路径为空，返回默认路径");
                return $"./{AutoBuilder.fallbackPath}";
            }
            var projectpath = AutoBuilder.assetsDirInfo.Parent.FullName;
            projectpath = projectpath.Replace("\\", "/").TrimEnd('/');
            stringValue = stringValue.Replace("\\", "/").TrimEnd('/');
            if (projectpath == stringValue)
            {
                Debug.LogWarning($"AutoBuilder: 路径工程根目录，返回默认路径");
                return $"./{AutoBuilder.fallbackPath}";
            }
            if (stringValue.StartsWith(projectpath))
            {
                stringValue = stringValue.Replace(projectpath, ".");
            }
            if (stringValue.StartsWith("./Assets"))
            {
                Debug.LogWarning($"AutoBuilder: 路径不能在 Assets 目录内，返回默认路径");
                return $"./{AutoBuilder.fallbackPath}";
            }
            return stringValue;
        }

        GUIContent content = new GUIContent("选择", "选择打包的应用存储的文件夹");
    }
}
