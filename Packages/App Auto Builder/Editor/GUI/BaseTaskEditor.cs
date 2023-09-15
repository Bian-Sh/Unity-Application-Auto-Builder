using UnityEditor;

public class BaseTaskEditor : Editor
{
    public BaseTask task;

    public virtual void OnEnable()
    {
        task = (BaseTask)target;
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
