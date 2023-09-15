using UnityEditor;

[CustomEditor(typeof(FunctionExecuteTask))]
public class FunctionExecuteTaskEditor : BaseTaskEditor
{
    public override void OnInspectorGUI()
    {
        //draw default inspector
        DrawDefaultInspector();
        DrawHelpbox();
    }
}
