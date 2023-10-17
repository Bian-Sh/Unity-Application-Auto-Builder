using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
//  this is a simple example allow you to change some data inside of a scene before build
// 这是一个简单的例子，允许你在构建前修改场景中的一些数据
//  for example, you have a method named Process in GameManager, you can fill it in.
// 例如你有一个方法叫 Process 在 GameManager 中，你可以填入。

[CreateAssetMenu(fileName = "FunctionTask", menuName = "Auto Builder/Task/Function Task")]

public class FunctionExecuteTask : BaseTask
{
    public SceneAsset scene;
    public MonoScript script;
    // 常规方法,比如 GameManager 中有 Process  方法  ，填入即可
    public string function;
    // 字段 args 是传入方法的参数,仅支持传入一个 string 
    public string args;
    SceneSetup[] previourScenes;

    private void OnEnable()
    {
        Description = @"方法执行任务非常的有用，可以在打包前或者打包后动态的修改指定场景中的变量并保存，PreBuild、PostBuild 配合使用可以使得修改只对指定的构建生效！
The FunctionExecute Task is a highly useful tool that allows you to modify data within a scene both before and after building, and then save those changes. By using prebuild and postbuild wisely, you can make targeted modifications for specific builds.";
    }
    public override void Run()
    {
        Debug.Log($"{nameof(FunctionExecuteTask)}: Run Function");
        //如果用户指定场景，我们加载场景中的函数
        // 否则，我们加载的函数必须认定用户执行的是静态函数，如果函数不存在则会报错，但不会影响构建
        if (scene)
        {
            // Save current open Scene into previours scene list , for re-open
            previourScenes = EditorSceneManager.GetSceneManagerSetup();
            // Open the scene
            var loadedScene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene));
            // Get the type of the class
            var type = script.GetClass();
            // Get the method
            var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var method = type.GetMethod(function, flag);
            // Get the instance of the class
            var instance = Object.FindObjectOfType(type);
            // Invoke the method
            method.Invoke(instance, new object[] { args });
            EditorSceneManager.SaveScene(loadedScene);
            // Re-open previours scenes
            EditorSceneManager.RestoreSceneManagerSetup(previourScenes);
        }
        else
        {
            var type = script.GetClass();
            var flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var method = type.GetMethod(function, flag);
            if (method != null)
            {
                method.Invoke(null, new object[] { args });
            }
            else
            {
                Debug.LogError($"{nameof(FunctionExecuteTask)}: 如果不指定场景，{function} 必须为静态方法!");
            }
        }
    }
}
