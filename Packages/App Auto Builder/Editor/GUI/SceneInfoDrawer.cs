using UnityEngine;
using UnityEditor;
namespace zFramework.Extension
{
    [CustomPropertyDrawer(typeof(SceneInfo))]
    public class SceneInfoDrawer : PropertyDrawer
    {
        GUIContent forenable = new GUIContent("", "是否将这个场景打包");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect r1 = position;
            r1.width = 20;
            Rect r2 = position;
            r2.xMin = r1.xMax + 10;
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(r1, property.FindPropertyRelative("enabled"), forenable);
            EditorGUI.PropertyField(r2, property.FindPropertyRelative("scene"), GUIContent.none);
            EditorGUI.EndProperty();
        }
    }
}
