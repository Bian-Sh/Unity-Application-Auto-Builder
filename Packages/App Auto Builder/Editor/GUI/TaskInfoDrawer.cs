using UnityEngine;
using UnityEditor;
namespace zFramework.Extension
{
    [CustomPropertyDrawer(typeof(TaskInfo))]
    public class TaskInfoDrawer : PropertyDrawer
    {
        GUIContent forenable = new GUIContent("", "是否启用这个任务");
        GUIContent forbutton = new GUIContent("Properties", "打开任务的属性面板");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect r1 = position;
            r1.width = 20;
            EditorGUI.PropertyField(r1, property.FindPropertyRelative("enabled"), forenable);

            Rect r2 = position;
            r2.xMin = r1.xMax + 10;
            r2.width -= r1.width + 60;
            EditorGUI.PropertyField(r2, property.FindPropertyRelative("task"), GUIContent.none);

            //Draw a button on the right to open the properties panel of the task's scriptable object.
            Rect r3 = position;
            r3.xMin = r2.xMax + 10;
            if (GUI.Button(r3, forbutton, EditorStyles.miniButton))
            {
                var task = property.FindPropertyRelative("task");
                var taskobj = task.objectReferenceValue;
                if (taskobj != null)
                {
                    EditorGUIUtility.PingObject(taskobj);
                    Selection.activeObject = taskobj;
#if UNITY_2020_2_OR_NEWER 
                    //open the properties panel of the task's scriptable object.
                    EditorApplication.ExecuteMenuItem("Assets/Properties...");
#endif
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
