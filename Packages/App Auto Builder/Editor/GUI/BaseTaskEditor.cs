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
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button(GUIContent, GUILayout.Height(30), GUILayout.Width(120)))
        {
            task.Run();
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
