using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[CreateAssetMenu(fileName = "FuncTask", menuName = "Build Tool/FuncTask")]
public class RunFunctionTask : BaseTask
{
    public SceneAsset scene;
    public MonoScript script;
    // 常规方法,比如 GameManager 中有 Process  方法  ，填入即可
    public string function;
    // 字段 args 是传入方法的参数,仅支持传入一个 string 
    public string args;
    SceneSetup[] previourScenes;
    public override void Run()
    {
        Debug.Log($"{nameof(RunFunctionTask)}: Run Function");
        // Save current open Scene into previours scene list , for re-open
        previourScenes = EditorSceneManager.GetSceneManagerSetup();
        // Open the scene
        var loadedScene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene));
        // Get the type of the class
        var type = script.GetClass();
        // Get the method
        var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var method = type.GetMethod(function,flag);
        // Get the instance of the class
        var instance = Object.FindObjectOfType(type);
        // Invoke the method
        method.Invoke(instance, new object[] { args });
        EditorSceneManager.SaveScene(loadedScene);
        // Re-open previours scenes
        EditorSceneManager.RestoreSceneManagerSetup(previourScenes);
    }
}
