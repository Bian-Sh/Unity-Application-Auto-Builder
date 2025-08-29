using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using zFramework.AppBuilder.Utils;

namespace zFramework.AppBuilder
{
    /// <summary>
    ///  用于移除 Windows 平台的启动画面
    ///  此功能需要 PlayerSettings 中的 Draw Mode 改为 All Sequential ，且保持 Unity Logo 为首个
    ///  此任务发生在打包完成后，我们默认移除首个 Unity Logo 画面
    /// </summary>
    [CreateAssetMenu(fileName = "WinSplashRemoveTask", menuName = "Auto Builder/Task/WinSplashRemoveTask", order = 1)]
    public class WinSplashRemoveTask : BaseTask
    {
        private void OnEnable()
        {
            taskType = TaskType.PostBuild;
            Description = @"移除 Windows 平台的启动画面
请注意：
1. 仅在 Windows 平台下生效, 并且 PlayerSettings 中的 Draw Mode 为 All Sequential
2. 其他平台请参考本案例自行实现
3. 如果有使用 Virbox Encrypt Task，请确保此任务优先于 Virbox Encrypt Task 执行，priority 值越小优先级越高
4. 支持 Unity 2022.3 及以上版本
";
        }

        public override bool Validate()
        {
            //至少 PlayerSettings 中的 Draw Mode 为 All Sequential
            bool hasAllSequentialEnabled = PlayerSettings.SplashScreen.drawMode == PlayerSettings.SplashScreen.DrawMode.AllSequential;
            if (!hasAllSequentialEnabled)
            {
                Debug.LogError("WinSplashRemoveTask 验证失败: PlayerSettings 中的 Draw Mode 必须为 All Sequential");
            }
            // 对 Build Target 进行检查
            var isValidBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
                                  EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64;
            if (!isValidBuildTarget)
            {
                Debug.LogError("WinSplashRemoveTask 验证失败: 仅支持 Windows 平台，如需支持其他平台，可参考本代码自行实现！");
            }
            return hasAllSequentialEnabled && isValidBuildTarget;
        }

        public override async Task<BuildTaskResult> RunAsync(string exeFile)
        {
            // 传入的 output 是最终的 exe 路径，传入即可，UDSR 会自动处理
            var (Success, Reason) = await RemoveUnitySplashWithUDSRAsync(exeFile);
            var result = new BuildTaskResult(Success, exeFile, Success ? null : Reason);
            if (Success)
            {
                Debug.Log($"默认闪屏移除成功, {Reason}");
            }
            else
            {
                Debug.LogError($"移除闪屏时发生错误: {Reason}");
            }
            return result;
        }

        /// <summary>
        /// 使用 UDSR.exe 移除 Unity Splash Screen
        /// </summary>
        private async Task<(bool Success, string Reason)> RemoveUnitySplashWithUDSRAsync(string globalgamemanagersPath)
        {
            if (string.IsNullOrWhiteSpace(globalgamemanagersPath) || !File.Exists(globalgamemanagersPath))
                return (false, $"globalgamemanagers 文件不存在: {globalgamemanagersPath}");
            string udsrPath;
            try
            {
                udsrPath = GetUDSRPath();
            }
            catch (Exception ex)
            {
                return (false, $"UDSR 路径错误: {ex.Message}");
            }
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = udsrPath,
                Arguments = $"\"{globalgamemanagersPath}\"", // 只传入 globalgamemanagers 路径
                CreateNoWindow = true
            };
            var program = new Program(startInfo);
            program.OnStandardOutputReceived += line =>Debug.Log(line);
            program.OnStandardErrorReceived += line => Debug.LogError(line);
            await program.StartAsync();
            int exitCode = program.ExitCode;
            if (exitCode == 0)
            {
                return (true, $"UDSR 执行成功!");
            }
            else
            {
                return (false, $"UDSR 执行失败，ExitCode={exitCode}。");
            }
        }

        private string GetUDSRPath()
        {
            var scriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
            var pluginsDir = Path.Combine(Path.GetDirectoryName(scriptPath), "../../Binaries");
            var udsrFile = Path.Combine(pluginsDir, "UDSR.exe");
            if (!File.Exists(udsrFile))
            {
                throw new FileNotFoundException($"UDSR 文件不存在: {udsrFile}");
            }
            return udsrFile;
        }
    }
}