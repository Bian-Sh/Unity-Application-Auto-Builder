using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string platform = "Default Platform";

    private void Update()
    {
        Debug.LogError($"{nameof(GameManager)}: platform = {platform}");
    }

    
#if UNITY_EDITOR
    // this is the function that will be called by the RunFunctionTask
    // if the field "platform" is not equal to args that you set in the RunFunctionTask, it will be changed to args
    // if the field "platform" on the inspector is equal to args that you set in the RunFunctionTask,  you can reset it to default value to see the effect gain

    // 这个函数会被 RunFunctionTask 调用
    // 如果 platform 字段不等于 RunFunctionTask 中的 args 字段，那么 platform 字段会被修改为 args 字段
    // 如果 Inspector 上 platform 数值等于 RunFunctionTask 中的 args 字段，那么你可以重置 platform 字段为默认值来查看效果
    
    private void SomeFunction(string args)
    {
        platform = args;
        Debug.Log($"{nameof(GameManager)}: Function called with args: {args}");
    }
#endif
}
