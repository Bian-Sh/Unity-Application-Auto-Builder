using UnityEngine;

public class BaseTask : ScriptableObject
{
    public TaskType taskType;
    public int priority;
    public virtual void Run()
    {
        throw new System.NotImplementedException();
    }
}
