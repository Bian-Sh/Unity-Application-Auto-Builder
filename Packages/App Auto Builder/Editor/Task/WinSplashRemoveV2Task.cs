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
    [CreateAssetMenu(fileName = "WinSplashRemoveV2Task", menuName = "Auto Builder/Task/WinSplashRemoveV2Task", order = 1)]
    public class WinSplashRemoveV2Task : BaseTask
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
5. 如果发现执行失败，请及时更新 classdata.tpk 文件以确保 TypeTree 与资产内存布局一致
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
            // 传入的 output 是最终的 exe 路径，我们需要找到同目录下的 Data 目录，继而找到  globalgamemanagers 文件
            // 本函数使用 AssetsTools.Net 来处理 globalgamemanagers 文件
            string exeDir = Path.GetDirectoryName(exeFile);
            string dataDir = Path.Combine(exeDir, $"{Path.GetFileNameWithoutExtension(exeFile)}_Data");
            string globalgamemanagersPath = Path.Combine(dataDir, "globalgamemanagers");
            Debug.Log($"[WinSplashRemoveV2Task] globalgamemanagers 路径: {globalgamemanagersPath}");
            var (Success, Reason) = await Task.Run(() => RemoveUnitySplash(globalgamemanagersPath));
            Debug.Log($"[WinSplashRemoveV2Task] RunAsync 结束，Success: {Success}, Reason: {Reason}");
            var result = new BuildTaskResult(Success, exeFile, Success ? null : Reason);
            return result;
        }

        /// <summary>
        /// 移除 Unity Splash Screen (仅支持 Windows globalgamemanagers 文件)
        /// </summary>
        /// <param name="globalgamemanagersPath">globalgamemanagers 文件路径</param>
        /// <returns>操作结果和原因</returns>
        public static (bool Success, string Reason) RemoveUnitySplash(string globalgamemanagersPath)
        {
            List<string> temporaryFiles = new();
            try
            {
                if (string.IsNullOrWhiteSpace(globalgamemanagersPath))
                {
                    Debug.LogError("[WinSplashRemoveV2Task] 文件路径不能为空");
                    return (false, "文件路径不能为空");
                }
                if (!File.Exists(globalgamemanagersPath))
                {
                    Debug.LogError($"[WinSplashRemoveV2Task] 文件不存在: {globalgamemanagersPath}");
                    return (false, $"文件不存在: {globalgamemanagersPath}");
                }
                string fileName = Path.GetFileName(globalgamemanagersPath);
                if (!fileName.Contains("globalgamemanagers"))
                {
                    Debug.LogError("[WinSplashRemoveV2Task] 不支持的文件类型，仅支持 globalgamemanagers 文件");
                    return (false, "不支持的文件类型，仅支持 globalgamemanagers 文件");
                }
                // tpkFile 路径获取逻辑保持原样
                var scriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
                var pluginsDir = Path.Combine(Path.GetDirectoryName(scriptPath), "../../Binaries");
                var tpkFile = Path.Combine(pluginsDir, "classdata.tpk");
                Debug.Log($"[WinSplashRemoveV2Task] tpkFile 路径: {tpkFile}");
                if (!File.Exists(tpkFile))
                {
                    Debug.LogError($"[WinSplashRemoveV2Task] TPK 文件不存在: {tpkFile}");
                    return (false, $"TPK 文件不存在: {tpkFile}");
                }
                string backupFile = $"{globalgamemanagersPath}.bak";
                if (!File.Exists(backupFile))
                {
                    try
                    {
                        File.Copy(globalgamemanagersPath, backupFile, false);
                        Debug.Log($"[WinSplashRemoveV2Task] 备份文件已创建: {backupFile}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[WinSplashRemoveV2Task] 创建备份文件失败: {ex.Message}");
                        return (false, $"创建备份文件失败: {ex.Message}");
                    }
                }
                string tempFile = $"{globalgamemanagersPath}.temp";
                temporaryFiles.Add(tempFile);
                try
                {
                    File.Copy(globalgamemanagersPath, tempFile, true);
                    Debug.Log($"[WinSplashRemoveV2Task] 临时文件已创建: {tempFile}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WinSplashRemoveV2Task] 创建临时文件失败: {ex.Message}");
                    return (false, $"创建临时文件失败: {ex.Message}");
                }
                AssetsManager assetsManager = new();
                AssetsFileInstance assetFileInstance = null;
                try
                {
                    assetsManager.LoadClassPackage(path: tpkFile);
                    Debug.Log("[WinSplashRemoveV2Task] ClassPackage 加载完成");
                    assetFileInstance = assetsManager.LoadAssetsFile(tempFile, true);
                    if (assetFileInstance == null)
                    {
                        Debug.LogError("[WinSplashRemoveV2Task] 加载资源文件失败");
                        return (false, "加载资源文件失败");
                    }
                    Debug.Log("[WinSplashRemoveV2Task] AssetsFile 加载完成");
                    assetsManager.LoadClassDatabaseFromPackage(assetFileInstance.file.Metadata.UnityVersion);
                    Debug.Log($"[WinSplashRemoveV2Task] ClassDatabase 加载完成，UnityVersion: {assetFileInstance.file.Metadata.UnityVersion}");
                    var result = ProcessSplashRemoval(assetsManager, assetFileInstance);
                    if (!result.Success)
                    {
                        Debug.LogError($"[WinSplashRemoveV2Task] ProcessSplashRemoval 失败: {result.Reason}");
                        return result;
                    }
                    using (AssetsFileWriter writer = new(globalgamemanagersPath))
                    {
                        assetFileInstance.file.Write(writer);
                        Debug.Log($"[WinSplashRemoveV2Task] 写入 globalgamemanagers 完成: {globalgamemanagersPath}");
                    }
                    Debug.Log($"[WinSplashRemoveV2Task] RemoveUnitySplash 成功: {result.Reason}");
                    return (true, result.Reason);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WinSplashRemoveV2Task] 处理资源文件时出错: {ex.Message}");
                    return (false, $"处理资源文件时出错: {ex.Message}");
                }
                finally
                {
                    assetsManager?.UnloadAll(true);
                    Debug.Log("[WinSplashRemoveV2Task] 资源卸载完成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WinSplashRemoveV2Task] 未预期的错误: {ex.Message}");
                return (false, $"未预期的错误: {ex.Message}");
            }
            finally
            {
                foreach (string tempFile in temporaryFiles)
                {
                    try
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                            Debug.Log($"[WinSplashRemoveV2Task] 临时文件已删除: {tempFile}");
                        }
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 核心的 Splash Screen 移除逻辑
        /// </summary>
        private static (bool Success, string Reason) ProcessSplashRemoval(AssetsManager assetsManager, AssetsFileInstance assetFileInstance)
        {
            Debug.Log("[WinSplashRemoveV2Task] ProcessSplashRemoval 开始");
            try
            {
                AssetsFile assetFile = assetFileInstance.file;
                List<AssetFileInfo> buildSettingsInfos = assetFile.GetAssetsOfType(AssetClassID.BuildSettings);
                if (buildSettingsInfos == null || buildSettingsInfos.Count == 0)
                {
                    Debug.LogError("[WinSplashRemoveV2Task] 找不到 BuildSettings 数据");
                    return (false, "找不到 BuildSettings 数据");
                }
                AssetTypeValueField buildSettingsBase = assetsManager.GetBaseField(assetFileInstance, buildSettingsInfos[0]);
                List<AssetFileInfo> playerSettingsInfos = assetFile.GetAssetsOfType(AssetClassID.PlayerSettings);
                if (playerSettingsInfos == null || playerSettingsInfos.Count == 0)
                {
                    Debug.LogError("[WinSplashRemoveV2Task] 找不到 PlayerSettings 数据");
                    return (false, "找不到 PlayerSettings 数据");
                }
                AssetTypeValueField playerSettingsBase;
                try
                {
                    playerSettingsBase = assetsManager.GetBaseField(assetFileInstance, playerSettingsInfos[0]);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Type-Tree数据库与资产不匹配，请尝试从: https://nightly.link/AssetRipper/Tpk/workflows/type_tree_tpk/master/uncompressed_file.zip 手动下载替换 classdata.tpk, 或者更换 Unity 版本");
                    Debug.LogError($"[WinSplashRemoveV2Task] 无法获取 PlayerSettings 字段: {ex.Message}");
                    return (false, $"无法获取 PlayerSettings 字段: {ex.Message}。可能不支持当前的 Unity 版本");
                }
                bool hasProVersion = buildSettingsBase["hasPROVersion"].AsBool;
                bool showUnityLogo = playerSettingsBase["m_ShowUnitySplashLogo"].AsBool;
                Debug.Log($"[WinSplashRemoveV2Task] hasProVersion: {hasProVersion}, showUnityLogo: {showUnityLogo}");
                if (hasProVersion && !showUnityLogo)
                {
                    Debug.Log("[WinSplashRemoveV2Task] Unity 启动画面已经被移除过了");
                    return (true, "Unity 启动画面已经被移除过了");
                }
                AssetTypeValueField splashScreenLogos = playerSettingsBase["m_SplashScreenLogos.Array"];
                int totalSplashScreens = splashScreenLogos.Count();
                Debug.Log($"[WinSplashRemoveV2Task] SplashScreen 总数: {totalSplashScreens}");
                if (totalSplashScreens > 0)
                {
                    splashScreenLogos.Children.RemoveAt(0);
                    Debug.Log("[WinSplashRemoveV2Task] 已移除首个 SplashScreen");
                }
                buildSettingsBase["hasPROVersion"].AsBool = true;
                playerSettingsBase["m_ShowUnitySplashLogo"].AsBool = false;
                playerSettingsInfos[0].SetNewData(playerSettingsBase);
                buildSettingsInfos[0].SetNewData(buildSettingsBase);
                string message = totalSplashScreens > 0
                    ? $"成功移除首个（共 {totalSplashScreens} 个）Splash Screen 并设置为 Pro 版本"
                    : "已设置为 Pro 版本并隐藏 Unity Logo";
                Debug.Log($"[WinSplashRemoveV2Task] ProcessSplashRemoval 成功: {message}");
                return (true, message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WinSplashRemoveV2Task] 移除 Splash Screen 时发生错误: {ex.Message}");
                return (false, $"移除 Splash Screen 时发生错误: {ex.Message}");
            }
        }
    }
}