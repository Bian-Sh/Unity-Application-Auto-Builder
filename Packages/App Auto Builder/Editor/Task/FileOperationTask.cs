using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace zFramework.AppBuilder
{
    // todo: add a task to operate files for copy, delete, move, rename, create, etc.
    // todo: 添加一个任务来操作文件，比如复制，删除，移动，重命名等等
    [CreateAssetMenu(fileName = "FileOperationTask", menuName = "Auto Builder/Task/File Operation Task")]
    public partial class FileOperationTask : BaseTask
    {
        public FileOperationProfile[] operations; // array of file operations
        private void OnEnable()
        {
            Description = @"用于执行文件操作任务，包括复制、删除、移动、重命名等功能。

测试说明：
若需测试功能（如删除sourcePath指向的文件或目录），可按以下方式操作：
1. 预先创建对应的测试文件和目录结构
2. 在args中传入Unity打包后的输出路径（例如打包为exe时的路径：D:/Builds/MyGame.exe）
3. 配置sourcePath为项目内路径（如Assets/SomeFolder/SomeFile.txt）


执行预期：
任务运行时，对应打包输出目录下的文件（如D:/Builds/MyGame_Data/SomeFolder/SomeFile.txt）
将按操作类型（删除/复制等）执行相应处理，其他操作同理。

路径约定：
当路径以'Assets/'开头时，会自动替换为ProductName_Data目录路径。
ProductName 由 Path.GetFileNameWithoutExtension(output) 获取。

注意事项：
1. 确保操作的源路径和目标路径正确无误。
2. 确保有足够的权限执行文件操作。
3. 确保文件未被其他程序占用，否则可能导致操作失败。
4. 请确保重命名和移动操作的目标路径指向的文件不存在，否则会抛出异常。
5. 此任务所有逻辑仅供参考，实际使用时请根据项目需求自行调整逻辑代码。
";
        }

        public override async Task<BuildTaskResult> RunAsync(string output)
        {
            try
            {
                if (operations == null || operations.Length == 0)
                {
                    Debug.LogWarning($"{nameof(FileOperationTask)}: 没有配置任何操作");
                    return BuildTaskResult.Successful(output);
                }

                foreach (var operation in operations)
                {
                    await ExecuteOperation(operation, output);
                }

                return BuildTaskResult.Successful(output);
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(FileOperationTask)}: 执行失败 - {e.Message}");
                throw;
            }
        }

        private async Task ExecuteOperation(FileOperationProfile operation, string output)
        {
            try
            {
                var resolvedSourcePath = ResolvePath(operation.sourcePath, output);
                
                switch (operation.type)
                {
                    case FileOperationType.Copy:
                        await CopyOperation(resolvedSourcePath, ResolvePath(operation.destinationPath, output));
                        break;
                    case FileOperationType.Delete:
                        await DeleteOperation(resolvedSourcePath);
                        break;
                    case FileOperationType.Move:
                        await MoveOperation(resolvedSourcePath, ResolvePath(operation.destinationPath, output));
                        break;
                    case FileOperationType.Rename:
                        await RenameOperation(resolvedSourcePath, operation.newName);
                        break;
                }
                
                Debug.Log($"{nameof(FileOperationTask)}: {operation.type} 操作完成 - {resolvedSourcePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(FileOperationTask)}: {operation.type} 操作失败 - {e.Message}");
                throw;
            }
        }

        private string ResolvePath(string path, string output)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // 处理环境变量
            path = Environment.ExpandEnvironmentVariables(path);

            // 如果是绝对路径，直接返回
            if (Path.IsPathRooted(path))
                return path;

            // 获取output的目录路径
            var outputDir = Path.GetDirectoryName(output);
            if (string.IsNullOrEmpty(outputDir))
                outputDir = Directory.GetCurrentDirectory();

            // 如果路径以 "Assets/" 开头，替换为 ProductName_Data 目录
            var productName = Path.GetFileNameWithoutExtension(output);
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                outputDir = Path.Combine(outputDir, $"{productName}_Data");
                path = path["Assets/".Length..]; // 去掉 "Assets/" 前缀
            }

            // 拼接相对路径
            return Path.Combine(outputDir, path);
        }

        private async Task CopyOperation(string sourcePath, string destinationPath)
        {
            await Task.Run(() =>
            {
                if (File.Exists(sourcePath))
                {
                    // 复制文件
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    File.Copy(sourcePath, destinationPath, true);
                    ReportResult(destinationPath, () => $"{nameof(FileOperationTask)}: 文件复制完成 - ");
                }
                else if (Directory.Exists(sourcePath))
                {
                    // 复制目录
                    CopyDirectory(sourcePath, destinationPath);
                    ReportResult(destinationPath, () => $"{nameof(FileOperationTask)}: 目录复制完成 - ");
                }
                else
                {
                    throw new FileNotFoundException($"源路径不存在: {sourcePath}");
                }
            });
        }

        private async Task DeleteOperation(string sourcePath)
        {
            await Task.Run(() =>
            {
                if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                    Debug.Log($"{nameof(FileOperationTask)}: 文件删除完成 - {sourcePath}");
                }
                else if (Directory.Exists(sourcePath))
                {
                    Directory.Delete(sourcePath, true);
                    Debug.Log($"{nameof(FileOperationTask)}: 目录删除完成 - {sourcePath}");
                }
                else
                {
                    throw new FileNotFoundException($"源路径不存在: {sourcePath}");
                }
            });
        }

        private async Task MoveOperation(string sourcePath, string destinationPath)
        {
            await Task.Run(() =>
            {
                if (File.Exists(sourcePath))
                {
                    // 移动文件
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    File.Move(sourcePath, destinationPath);
                    ReportResult(destinationPath, () => $"{nameof(FileOperationTask)}: 文件移动完成 - ");
                }
                else if (Directory.Exists(sourcePath))
                {
                    // 移动目录
                    Directory.Move(sourcePath, destinationPath);
                    ReportResult(destinationPath, () => $"{nameof(FileOperationTask)}: 目录移动完成 - ");
                }
                else
                {
                    throw new FileNotFoundException($"源路径不存在: {sourcePath}");
                }
            });
        }

        private async Task RenameOperation(string sourcePath, string newName)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(newName))
                {
                    throw new ArgumentException("新名称不能为空");
                }

                var directory = Path.GetDirectoryName(sourcePath);
                var destinationPath = Path.Combine(directory, newName);

                if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, destinationPath);
                    ReportResult(destinationPath, () => $"{nameof(FileOperationTask)}: 文件重命名完成 - ");
                }
                else if (Directory.Exists(sourcePath))
                {
                    Directory.Move(sourcePath, destinationPath);
                    ReportResult(destinationPath, () => $"{nameof(FileOperationTask)}: 目录重命名完成 - ");
                }
                else
                {
                    throw new FileNotFoundException($"源路径不存在: {sourcePath}");
                }
            });
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var subDirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(targetDir, subDirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        public override bool Validate()
        {
            if (operations == null || operations.Length == 0)
            {
                Debug.LogWarning($"{nameof(FileOperationTask)}: 没有配置任何操作");
                return false;
            }

            bool isValid = true;
            for (int i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];
                
                // 检查源路径
                if (string.IsNullOrEmpty(operation.sourcePath))
                {
                    Debug.LogError($"{nameof(FileOperationTask)}: 操作 {i}: 源路径不能为空");
                    isValid = false;
                }

                // 根据操作类型检查必要字段
                switch (operation.type)
                {
                    case FileOperationType.Copy:
                    case FileOperationType.Move:
                        if (string.IsNullOrEmpty(operation.destinationPath))
                        {
                            Debug.LogError($"{nameof(FileOperationTask)}: 操作 {i}: {operation.type} 操作需要目标路径");
                            isValid = false;
                        }
                        break;
                    case FileOperationType.Rename:
                        if (string.IsNullOrEmpty(operation.newName))
                        {
                            Debug.LogError($"{nameof(FileOperationTask)}: 操作 {i}: 重命名操作需要新名称");
                            isValid = false;
                        }
                        break;
                    case FileOperationType.Delete:
                        // Delete 操作只需要源路径
                        break;
                }
            }

            return isValid;
        }
    }
}
