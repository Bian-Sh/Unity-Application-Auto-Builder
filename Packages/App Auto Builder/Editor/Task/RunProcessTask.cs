using UnityEngine;

//add a task to run a process, it is a simple task to run a process,without io redirect
//添加一个任务来运行一个进程, 这是一个简单的任务来运行一个进程，不带io重定向
[CreateAssetMenu(fileName = "ProcessTask", menuName = "Auto Builder/Task/Process Task")]
public class RunProcessTask : BaseTask
{
    public string exePath;
    public string args;
    public bool waitForExit;

    private void OnEnable()
    {
        Description = "在打包前后执行程序方便处理一些事务，应该会有用吧";
    }
    public override string Run(string output)
    {
        if (!string.IsNullOrEmpty(exePath))
        {
            Debug.Log($"{nameof(RunProcessTask)}: Run Process");
            var process = System.Diagnostics.Process.Start(exePath, this.args);
            if (waitForExit)
            {
                process.WaitForExit();
            }
        }
        return string.Empty;
    }
}
