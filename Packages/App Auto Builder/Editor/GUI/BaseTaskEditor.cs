using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(BaseTask), true)]
public class BaseTaskEditor : Editor
{
    public BaseTask task;
    public GUIContent GUIContent = new GUIContent("Test Run Task", "测试用户任务，请务必对自己的操作有认知能力！");
    public virtual void OnEnable()
    {
        task = (BaseTask)target;
    }

    string arg = string.Empty;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.selectionRect))
        {
            arg = EditorGUILayout.TextField("Args", arg);
            GUILayout.Space(4);
            var buttonRect = GUILayoutUtility.GetRect(GUIContent, GUI.skin.button, GUILayout.Height(24), GUILayout.Width(120));
            buttonRect.x = (EditorGUIUtility.currentViewWidth - buttonRect.width) / 2;

            if (GUI.Button(buttonRect, GUIContent))
            {
               var output = task.Run(arg);
                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log(output);
                }
            }

        }
        DrawHelpbox();
    }

    public void DrawHelpbox()
    {
        //draw a helpbox with the description
        if (!string.IsNullOrEmpty(task.Description))
        {
            EditorGUILayout.HelpBox(task.Description, MessageType.Info);
        }
    }

}
