using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
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

        public override async Task<BuildTaskResult> RunAsync(string output)
        {
            // 传入的 output 是最终的 exe 路径，我们需要找到同目录下的 Data 目录，继而找到  globalgamemanagers 文件
            // 本函数使用 AssetsTools.Net 来处理 globalgamemanagers 文件
            string exeDir = Path.GetDirectoryName(output);
            string dataDir = Path.Combine(exeDir, $"{Path.GetFileNameWithoutExtension(output)}_Data");
            string globalgamemanagersPath = Path.Combine(dataDir, "globalgamemanagers");
            var (Success, Reason) = await RemoveUnitySplashAsync(globalgamemanagersPath);
            var result = new BuildTaskResult(Success, output, Success ? null : Reason);
            Debug.Log(Success ? "Unity Splash Screen 移除成功" : $"Unity Splash Screen 移除失败: {Reason}");
            return result;
        }

        /// <summary>
        /// 异步移除 Unity Splash Screen (仅支持 Windows globalgamemanagers 文件)
        /// </summary>
        /// <param name="globalgamemanagersPath">globalgamemanagers 文件路径</param>
        /// <param name="classdataPath">classdata.tpk 文件路径，如果为空则使用程序目录下的文件</param>
        /// <returns>操作结果和原因</returns>
        public static async Task<(bool Success, string Reason)> RemoveUnitySplashAsync(string globalgamemanagersPath)
        {
            return await Task.Run(() =>
            {
                List<string> temporaryFiles = new();
                try
                {
                    // 1. 基本验证
                    if (string.IsNullOrWhiteSpace(globalgamemanagersPath))
                        return (false, "文件路径不能为空");

                    if (!File.Exists(globalgamemanagersPath))
                        return (false, $"文件不存在: {globalgamemanagersPath}");

                    string fileName = Path.GetFileName(globalgamemanagersPath);
                    if (!fileName.Contains("globalgamemanagers"))
                        return (false, "不支持的文件类型，仅支持 globalgamemanagers 文件");

                    // 2. 确定 classdata.tpk 路径
                    // 加载类数据库 
                    var scriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
                    var pluginsDir = Path.Combine(Path.GetDirectoryName(scriptPath), "../../Plugins");
                    // todo: 直接放在 Packages 下是可以找到此文件的，待确认以 .tgz 等方式安装后是否可以找到
                    var tpkFile = Path.Combine(pluginsDir, "classdata.tpk");

                    if (!File.Exists(tpkFile))
                        return (false, $"TPK 文件不存在: {tpkFile}");

                    // 3. 创建备份
                    string backupFile = $"{globalgamemanagersPath}.bak";
                    if (!File.Exists(backupFile))
                    {
                        try
                        {
                            File.Copy(globalgamemanagersPath, backupFile, false);
                        }
                        catch (Exception ex)
                        {
                            return (false, $"创建备份文件失败: {ex.Message}");
                        }
                    }

                    // 4. 创建临时文件
                    string tempFile = $"{globalgamemanagersPath}.temp_{Guid.NewGuid():N}";
                    temporaryFiles.Add(tempFile);

                    try
                    {
                        File.Copy(globalgamemanagersPath, tempFile, true);
                    }
                    catch (Exception ex)
                    {
                        return (false, $"创建临时文件失败: {ex.Message}");
                    }

                    // 5. 处理资源文件
                    AssetsManager assetsManager = new();
                    AssetsFileInstance assetFileInstance = null;

                    try
                    {
                        // 首先加载外部类数据包
                        assetsManager.LoadClassPackage(path: tpkFile);

                        // 加载资源文件
                        assetFileInstance = assetsManager.LoadAssetsFile(tempFile, true);
                        if (assetFileInstance == null)
                            return (false, "加载资源文件失败");

                        // 从包中加载对应Unity版本的类数据库
                        assetsManager.LoadClassDatabaseFromPackage(assetFileInstance.file.Metadata.UnityVersion);

                        // 执行核心移除逻辑
                        var result = ProcessSplashRemoval(assetsManager, assetFileInstance);
                        if (!result.Success)
                            return result;

                        // 保存到原文件
                        using (AssetsFileWriter writer = new(globalgamemanagersPath))
                        {
                            assetFileInstance.file.Write(writer);
                        }

                        return (true, result.Reason);
                    }
                    catch (Exception ex)
                    {
                        return (false, $"处理资源文件时出错: {ex.Message}");
                    }
                    finally
                    {
                        // 清理资源
                        assetsManager?.UnloadAll(true);
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"未预期的错误: {ex.Message}");
                }
                finally
                {
                    // 清理临时文件
                    foreach (string tempFile in temporaryFiles)
                    {
                        try
                        {
                            if (File.Exists(tempFile))
                                File.Delete(tempFile);
                        }
                        catch
                        {
                            // 忽略清理错误
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 核心的 Splash Screen 移除逻辑
        /// </summary>
        private static (bool Success, string Reason) ProcessSplashRemoval(
            AssetsManager assetsManager,
            AssetsFileInstance assetFileInstance)
        {
            try
            {
                AssetsFile assetFile = assetFileInstance.file;

                List<AssetFileInfo> buildSettingsInfos = assetFile.GetAssetsOfType(AssetClassID.BuildSettings);
                if (buildSettingsInfos == null || buildSettingsInfos.Count == 0)
                    return (false, "找不到 BuildSettings 数据");

                AssetTypeValueField buildSettingsBase = assetsManager.GetBaseField(assetFileInstance, buildSettingsInfos[0]);

                List<AssetFileInfo> playerSettingsInfos = assetFile.GetAssetsOfType(AssetClassID.PlayerSettings);
                if (playerSettingsInfos == null || playerSettingsInfos.Count == 0)
                    return (false, "找不到 PlayerSettings 数据");

                AssetTypeValueField playerSettingsBase;
                try
                {
                    playerSettingsBase = assetsManager.GetBaseField(
                        assetFileInstance, playerSettingsInfos[0]);
                }
                catch (Exception ex)
                {
                    return (false, $"无法获取 PlayerSettings 字段: {ex.Message}。可能不支持当前的 Unity 版本");
                }

                // 检查当前状态
                bool hasProVersion = buildSettingsBase["hasPROVersion"].AsBool;
                bool showUnityLogo = playerSettingsBase["m_ShowUnitySplashLogo"].AsBool;

                if (hasProVersion && !showUnityLogo) 
                {
                    // 如果已经是 Pro 版本并且隐藏了 Unity Logo，则无需再次处理，直接返回成功
                    Debug.Log("Unity Splash Screen 已经被移除过了");
                    return (true, string.Empty); 
                }

                // 获取并清除 splash screen logos
                AssetTypeValueField splashScreenLogos = playerSettingsBase["m_SplashScreenLogos.Array"];
                int totalSplashScreens = splashScreenLogos.Count();

                // 清除所有自定义 splash screens
                splashScreenLogos.Children.Clear();

                // 设置关键标志
                buildSettingsBase["hasPROVersion"].AsBool = true;
                playerSettingsBase["m_ShowUnitySplashLogo"].AsBool = false;

                // 保存更改
                playerSettingsInfos[0].SetNewData(playerSettingsBase);
                buildSettingsInfos[0].SetNewData(buildSettingsBase);

                string message = totalSplashScreens > 0
                    ? $"成功移除 {totalSplashScreens} 个自定义 Splash Screen 并设置为 Pro 版本"
                    : "已设置为 Pro 版本并隐藏 Unity Logo";

                return (true, message);
            }
            catch (Exception ex)
            {
                return (false, $"移除 Splash Screen 时发生错误: {ex.Message}");
            }
        }
    }
}