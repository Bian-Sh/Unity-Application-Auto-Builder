using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RunProcessTask))]
public class RunProcessTaskEditor : BaseTaskEditor
{
    GUIContent GUIContent = new GUIContent("Run Process Task", "Test run your process, without io redirect");

    public override void OnInspectorGUI()
    {
        //draw default inspector
        DrawDefaultInspector();
        // draw a button named "Test",tips "测试启动应用，风险自担~"
        if (GUILayout.Button(GUIContent, GUILayout.Height(30), GUILayout.Width(120)))
        {
            //run the task
            task.Run();
        }

        DrawHelpbox();
    }
}
