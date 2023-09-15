using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// todo: add a task to operate files for copy, delete, move, rename, create, etc.
// todo: 添加一个任务来操作文件，比如复制，删除，移动，重命名，创建等等
[CreateAssetMenu(fileName = "FileOperationTask", menuName = "Auto Builder/Task/File Operation Task")]
public class FileOperationTask : BaseTask
{


    public override void Run()
    {
        //hi guys,if you want to add a task to operate files, you can add it here.
        // 嗨，bro，如果你想添加一个操作文件的任务，你可以在这里添加。

    }

    // enum of file operation type
    // 文件操作类型的枚举
    public enum OperationType
    {
        Copy,
        Delete,
        Move,
        Rename,
        Create,
    }
}
