using System.IO;
using UnityEditor;
using UnityEngine;
namespace zFramework.AppBuilder
{
    /// <summary>
    /// Show in explorer for a folder path
    /// </summary>
    public class ShowInExplorerAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ShowInExplorerAttribute))]
    public class ShowInExplorerAttributeDrawer : PropertyDrawer
    {
        GUIContent content = new GUIContent("Show", "Show in explorer");
        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _lable)
        {
            var rect = new Rect(_position);
            rect.width -= 62;
            _property.stringValue = EditorGUI.TextField(rect, _property.displayName, _property.stringValue);
            var rect_bt = new Rect(_position);
            rect_bt.x = rect_bt.xMax - (EditorGUIUtility.hierarchyMode ? 50 : 60);
            rect_bt.width = 58;
            if (GUI.Button(rect_bt, content))
            {
                var defaultpath = _property.serializedObject.FindProperty("appLocationPath").stringValue;
                if (string.IsNullOrEmpty(defaultpath))
                {
                    //log warning
                    Debug.LogWarning("Root Path is empty, please set root path first!");
                    return;
                }
                var subpath = _property.stringValue;
                var fullpath = Path.Combine(defaultpath, subpath);
                if (!Directory.Exists(fullpath))
                {
                    //log warning
                    Debug.LogWarning("Path not exist, please build your app first !");
                    return;
                }
                // show in explorer
                EditorUtility.RevealInFinder(fullpath);
            }
        }
    }
}