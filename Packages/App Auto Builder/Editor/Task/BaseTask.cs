using UnityEngine;

public class BaseTask : ScriptableObject
{
    public TaskType taskType;
    public int priority;
    internal string Description;
    public virtual void Run()
    {
        throw new System.NotImplementedException();
    }
}
