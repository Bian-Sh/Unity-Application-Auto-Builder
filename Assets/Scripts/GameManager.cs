using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string platform = "Default Platform";

    private void Update()
    {
        Debug.Log($"{nameof(GameManager)}: platform = {platform}");
    }

    
#if UNITY_EDITOR
    private void SomeFunction(string args)
    {
        platform = args;
        Debug.Log($"{nameof(GameManager)}: Function called with args: {args}");
    }
#endif
}
