using UnityEditor;
using UnityEngine;
namespace zFramework.Extension
{
    public class OpenFolderAttribute : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(OpenFolderAttribute))]
    public class OpenFolderAttributeDrawer : PropertyDrawer
    {
        GUIContent content = new GUIContent("选择", "选择打包的应用存储的文件夹");
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
                var defaultpath = string.IsNullOrEmpty(_property.stringValue) ? Application.dataPath : _property.stringValue;
                var path = EditorUtility.OpenFolderPanel("请选择路径", defaultpath, string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    GUIUtility.keyboardControl = 0;
                    _property.stringValue = path;
                }
            }
        }
    }
}
