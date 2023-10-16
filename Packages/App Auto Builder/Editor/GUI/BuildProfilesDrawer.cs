using UnityEditor;
using UnityEngine;
namespace zFramework.Extension
{
    [CustomPropertyDrawer(typeof(BuildProfiles))]
    public class BuildProfilesDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // begin drawer property
            var label_p = EditorGUI.BeginProperty(position, label, property);

            // draw a foldout
            var rect_foldout = position;
            rect_foldout.height = EditorGUIUtility.singleLineHeight;
            Rect rect_buildstate = rect_foldout;
            // 当此 so 在 editorwindow 中被加载，需要向左缩进 10
            if (!EditorGUIUtility.hierarchyMode)
            {
                rect_foldout.xMin -= 12;
            }
            //  将 platform 绘制成 label 后缀 ，例如：OneApp(Windows64)
            var platform = property.FindPropertyRelative("platform");
            var appname = property.FindPropertyRelative("productName").stringValue;
            var label_platform = $"{appname}({platform.enumDisplayNames[platform.enumValueIndex]})";
            label_p.text = label_platform;

            // 将 isbuild 绘制在 foldout 右侧，靠右对齐
            rect_buildstate.x = position.width - 60;
            rect_buildstate.width = 100;
            var isbuild = property.FindPropertyRelative("isBuild");
            var label_isbuild = $"Need be build {(isbuild.boolValue && platform.enumValueIndex != 0 ? "<b><color=green>√</color></b>" : "<b><color=red>×</color></b>")}";
            var cached = GUI.skin.label.richText;
            GUI.skin.label.richText = true;
            GUI.Label(rect_buildstate, label_isbuild);
            GUI.skin.label.richText = cached;

            property.isExpanded = EditorGUI.Foldout(rect_foldout, property.isExpanded, label_p, EditorStyles.boldFont);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }


            //local function for draw a serialized property
            bool DrawProperty(ref Rect rect, string propertyName)
            {
                var prop = property.FindPropertyRelative(propertyName);
                rect.y += rect.height;
                rect.height = EditorGUI.GetPropertyHeight(prop);
                return EditorGUI.PropertyField(rect, prop, true);
            }
            Rect rect = position;
            rect.height = EditorGUIUtility.singleLineHeight;     // skip foldout
            DrawProperty(ref rect, "productName");                  // draw productName
            DrawProperty(ref rect, "isBuild");                               // draw isBuild
            DrawProperty(ref rect, "platform");                           // draw platform
            DrawProperty(ref rect, "saveLocation");                   // draw saveLocation
            DrawProperty(ref rect, "productVersion");               // draw productVersion
            DrawProperty(ref rect, "buildOptions");                   // draw buildOptions
            DrawProperty(ref rect, "scenes");                               // draw scenes
            DrawProperty(ref rect, "customTask");                     // draw customTask

            EditorGUI.EndProperty();                                              // end drawer property
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            var height = EditorGUI.GetPropertyHeight(property);
            return height;
        }
    }
}
