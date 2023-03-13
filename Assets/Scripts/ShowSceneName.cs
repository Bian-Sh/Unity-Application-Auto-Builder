using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShowSceneName : MonoBehaviour
{
    [SerializeField]
#pragma warning disable IDE0044 // 添加只读修饰符
    Text text;
#pragma warning restore IDE0044 // 添加只读修饰符
    void Start()
    {
        var str = $"当前场景名：{SceneManager.GetActiveScene().name}\n";
        var count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            str += $"{nameof(ShowSceneName)}: 打包的场景有：{name}\n";
        }
        text.text = str;
    }
}
