using UnityEngine;

public class BaseTask : ScriptableObject
{
    public TaskType taskType;
    public int priority;
    internal string Description;
    public virtual string Run(string output)
    {
        throw new System.NotImplementedException();
    }
}
