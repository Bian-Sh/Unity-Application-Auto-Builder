using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace zFramework.AppBuilder
{
    /// <summary>
    /// 用于移除 Unity 水印（底部右下角的文本）
    /// 此功能通过修改 BuildSettings 中的 isNoWatermarkBuild 和 isTrial 字段来实现
    /// 此任务发生在打包完成后
    /// </summary>
    [CreateAssetMenu(fileName = "WaterMarkRemoveTask", menuName = "Auto Builder/Task/WaterMarkRemoveTask", order = 2)]
    public class WaterMarkRemoveTask : BaseTask
    {
        private void OnEnable()
        {
            taskType = TaskType.PostBuild;
            Description = @"移除 Unity 水印（底部右下角的文本）
请注意：
1. 仅在 Windows 平台下生效
2. 其他平台请参考本案例自行实现
3. 如果有使用 Virbox Encrypt Task，请确保此任务优先于 Virbox Encrypt Task 执行，priority 值越小优先级越高
4. 支持 Unity 2022.3 及以上版本
5. 如果发现执行失败，请及时更新 classdata.tpk 文件以确保 TypeTree 与资产内存布局一致
";
        }

        public override bool Validate()
        {
            // 对 Build Target 进行检查
            var isValidBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
                                  EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64;
            if (!isValidBuildTarget)
            {
                Debug.LogError("WaterMarkRemoveTask 验证失败: 仅支持 Windows 平台，如需支持其他平台，可参考本代码自行实现！");
            }
            return isValidBuildTarget;
        }

        public override async Task<BuildTaskResult> RunAsync(string exeFile)
        {
            // 传入的 output 是最终的 exe 路径，我们需要找到同目录下的 Data 目录，继而找到 globalgamemanagers 文件
            // 本函数使用 AssetsTools.Net 来处理 globalgamemanagers 文件
            string exeDir = Path.GetDirectoryName(exeFile);
            string dataDir = Path.Combine(exeDir, $"{Path.GetFileNameWithoutExtension(exeFile)}_Data");
            string globalgamemanagersPath = Path.Combine(dataDir, "globalgamemanagers");
            Debug.Log($"[WaterMarkRemoveTask] globalgamemanagers 路径: {globalgamemanagersPath}");
            var (Success, Reason) = await Task.Run(() => RemoveUnityWatermark(globalgamemanagersPath));
            Debug.Log($"[WaterMarkRemoveTask] RunAsync 结束，Success: {Success}, Reason: {Reason}");
            var result = new BuildTaskResult(Success, exeFile, Success ? null : Reason);
            return result;
        }

        /// <summary>
        /// 移除 Unity 水印 (仅支持 Windows globalgamemanagers 文件)
        /// </summary>
        /// <param name="globalgamemanagersPath">globalgamemanagers 文件路径</param>
        /// <returns>操作结果和原因</returns>
        public static (bool Success, string Reason) RemoveUnityWatermark(string globalgamemanagersPath)
        {
            List<string> temporaryFiles = new();
            try
            {
                if (string.IsNullOrWhiteSpace(globalgamemanagersPath))
                {
                    Debug.LogError("[WaterMarkRemoveTask] 文件路径不能为空");
                    return (false, "文件路径不能为空");
                }
                if (!File.Exists(globalgamemanagersPath))
                {
                    Debug.LogError($"[WaterMarkRemoveTask] 文件不存在: {globalgamemanagersPath}");
                    return (false, $"文件不存在: {globalgamemanagersPath}");
                }
                string fileName = Path.GetFileName(globalgamemanagersPath);
                if (!fileName.Contains("globalgamemanagers"))
                {
                    Debug.LogError("[WaterMarkRemoveTask] 不支持的文件类型，仅支持 globalgamemanagers 文件");
                    return (false, "不支持的文件类型，仅支持 globalgamemanagers 文件");
                }

                // tpkFile 路径获取逻辑保持原样
                var scriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
                var pluginsDir = Path.Combine(Path.GetDirectoryName(scriptPath), "../../Binaries");
                var tpkFile = Path.Combine(pluginsDir, "classdata.tpk");
                Debug.Log($"[WaterMarkRemoveTask] tpkFile 路径: {tpkFile}");
                if (!File.Exists(tpkFile))
                {
                    Debug.LogError($"[WaterMarkRemoveTask] TPK 文件不存在: {tpkFile}");
                    return (false, $"TPK 文件不存在: {tpkFile}");
                }

                string backupFile = $"{globalgamemanagersPath}.watermark.bak";
                if (!File.Exists(backupFile))
                {
                    try
                    {
                        File.Copy(globalgamemanagersPath, backupFile, false);
                        Debug.Log($"[WaterMarkRemoveTask] 备份文件已创建: {backupFile}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[WaterMarkRemoveTask] 创建备份文件失败: {ex.Message}");
                        return (false, $"创建备份文件失败: {ex.Message}");
                    }
                }

                string tempFile = $"{globalgamemanagersPath}.watermark.temp";
                temporaryFiles.Add(tempFile);
                try
                {
                    File.Copy(globalgamemanagersPath, tempFile, true);
                    Debug.Log($"[WaterMarkRemoveTask] 临时文件已创建: {tempFile}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WaterMarkRemoveTask] 创建临时文件失败: {ex.Message}");
                    return (false, $"创建临时文件失败: {ex.Message}");
                }

                AssetsManager assetsManager = new();
                AssetsFileInstance assetFileInstance = null;
                try
                {
                    assetsManager.LoadClassPackage(path: tpkFile);
                    Debug.Log("[WaterMarkRemoveTask] ClassPackage 加载完成");
                    assetFileInstance = assetsManager.LoadAssetsFile(tempFile, true);
                    if (assetFileInstance == null)
                    {
                        Debug.LogError("[WaterMarkRemoveTask] 加载资源文件失败");
                        return (false, "加载资源文件失败");
                    }
                    Debug.Log("[WaterMarkRemoveTask] AssetsFile 加载完成");
                    assetsManager.LoadClassDatabaseFromPackage(assetFileInstance.file.Metadata.UnityVersion);
                    Debug.Log($"[WaterMarkRemoveTask] ClassDatabase 加载完成，UnityVersion: {assetFileInstance.file.Metadata.UnityVersion}");

                    var result = ProcessWatermarkRemoval(assetsManager, assetFileInstance);
                    if (!result.Success)
                    {
                        Debug.LogError($"[WaterMarkRemoveTask] ProcessWatermarkRemoval 失败: {result.Reason}");
                        return result;
                    }

                    using (AssetsFileWriter writer = new(globalgamemanagersPath))
                    {
                        assetFileInstance.file.Write(writer);
                        Debug.Log($"[WaterMarkRemoveTask] 写入 globalgamemanagers 完成: {globalgamemanagersPath}");
                    }

                    Debug.Log($"[WaterMarkRemoveTask] RemoveUnityWatermark 成功: {result.Reason}");
                    return (true, result.Reason);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WaterMarkRemoveTask] 处理资源文件时出错: {ex.Message}");
                    return (false, $"处理资源文件时出错: {ex.Message}");
                }
                finally
                {
                    assetsManager?.UnloadAll(true);
                    Debug.Log("[WaterMarkRemoveTask] 资源卸载完成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaterMarkRemoveTask] 未预期的错误: {ex.Message}");
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
                            Debug.Log($"[WaterMarkRemoveTask] 临时文件已删除: {tempFile}");
                        }
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 核心的水印移除逻辑
        /// </summary>
        private static (bool Success, string Reason) ProcessWatermarkRemoval(AssetsManager assetsManager, AssetsFileInstance assetFileInstance)
        {
            Debug.Log("[WaterMarkRemoveTask] ProcessWatermarkRemoval 开始");
            try
            {
                AssetsFile assetFile = assetFileInstance.file;
                List<AssetFileInfo> buildSettingsInfos = assetFile.GetAssetsOfType(AssetClassID.BuildSettings);
                if (buildSettingsInfos == null || buildSettingsInfos.Count == 0)
                {
                    Debug.LogError("[WaterMarkRemoveTask] 找不到 BuildSettings 数据");
                    return (false, "找不到 BuildSettings 数据");
                }

                AssetTypeValueField buildSettingsBase;
                try
                {
                    buildSettingsBase = assetsManager.GetBaseField(assetFileInstance, buildSettingsInfos[0]);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Type-Tree数据库与资产不匹配，请尝试从: https://nightly.link/AssetRipper/Tpk/workflows/type_tree_tpk/master/uncompressed_file.zip 手动下载替换 classdata.tpk, 或者更换 Unity 版本");
                    Debug.LogError($"[WaterMarkRemoveTask] 无法获取 BuildSettings 字段: {ex.Message}");
                    return (false, $"无法获取 BuildSettings 字段: {ex.Message}。可能不支持当前的 Unity 版本");
                }

                bool noWatermark = buildSettingsBase["isNoWatermarkBuild"].AsBool;
                bool isTrial = buildSettingsBase["isTrial"].AsBool;

                Debug.Log($"[WaterMarkRemoveTask] isNoWatermarkBuild: {noWatermark}, isTrial: {isTrial}");

                if (noWatermark && !isTrial)
                {
                    Debug.Log("[WaterMarkRemoveTask] 水印已经被移除过了");
                    return (true, "水印已经被移除过了");
                }

                // 设置为无水印构建并且非试用版
                buildSettingsBase["isNoWatermarkBuild"].AsBool = true;
                buildSettingsBase["isTrial"].AsBool = false;
                buildSettingsInfos[0].SetNewData(buildSettingsBase);

                string message = "成功移除 Unity 水印";
                Debug.Log($"[WaterMarkRemoveTask] ProcessWatermarkRemoval 成功: {message}");
                return (true, message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaterMarkRemoveTask] 移除水印时发生错误: {ex.Message}");
                return (false, $"移除水印时发生错误: {ex.Message}");
            }
        }
    }
}