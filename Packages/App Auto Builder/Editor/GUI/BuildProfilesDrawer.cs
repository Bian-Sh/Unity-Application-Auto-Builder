using System.Collections;
using System.Collections.Generic;
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
            var serializedObject = property.serializedObject;
            serializedObject.UpdateIfRequiredOrScript();

            // draw a foldout
            Rect rect = position;
            rect.height = EditorGUIUtility.singleLineHeight;
            Rect rect_buildstate = rect;

            // 将 isbuild 绘制在 foldout 右侧，靠右对齐
            rect_buildstate.x = position.width - 100;
            rect_buildstate.width = 100;
            var isbuild = property.FindPropertyRelative("isBuild");
            var label_isbuild = $"Need be build {(isbuild.boolValue ? "<b><color=green>√</color></b>" : "<b><color=red>×</color></b>")}";
            var cached = GUI.skin.label.richText;
            GUI.skin.label.richText = true;
            GUI.Label(rect_buildstate, label_isbuild);
            GUI.skin.label.richText = cached;

            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label_p);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.BeginChangeCheck();

            //local function for draw a serialized property
            bool DrawProperty(ref Rect rect, string propertyName)
            {
                var prop = property.FindPropertyRelative(propertyName);
                rect.y += rect.height;
                rect.height = EditorGUI.GetPropertyHeight(prop);
                return EditorGUI.PropertyField(rect, prop,true);
            }

            DrawProperty(ref rect, "productName");                  // draw productName
            DrawProperty(ref rect, "productVersion");               // draw productVersion
            DrawProperty(ref rect, "isBuild");                               // draw isBuild
            DrawProperty(ref rect, "buildOptions");                   // draw buildOptions
            DrawProperty(ref rect, "scenes");                              // draw scenes
            DrawProperty(ref rect, "customTask");                    // draw customTask

            var result = EditorGUI.EndChangeCheck();
            if (result)
            {
                serializedObject.ApplyModifiedProperties();
            }
            // end drawer property
            EditorGUI.EndProperty();
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
