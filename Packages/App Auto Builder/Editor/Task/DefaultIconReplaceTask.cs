using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Linq;
using UnityEditorInternal;

namespace zFramework.AppBuilder
{
    [CreateAssetMenu(fileName = "DefaultIconReplaceTask", menuName = "Auto Builder/Task/Default Icon Replace Task")]
    public class DefaultIconReplaceTask : BaseTask
    {
        [Header("图标设置")]
        [Tooltip("要替换的默认图标纹理，置空则使用Unity默认图标")]
        public Texture2D defaultIcon;

        private void OnEnable()
        {
            taskType = TaskType.PreBuild;
            Description = @"替换 PlayerSettings 的 Default Icon

功能说明：
1. 在构建前将指定的图标设置为应用的默认图标
2. 如果图标为空，则使用Unity默认图标

使用说明：
1. 在 defaultIcon 字段中指定要使用的默认图标
2. 如果要恢复Unity默认图标，将 defaultIcon 置空即可
3. 建议图标尺寸为 2 的幂次方（如 32x32, 64x64, 128x128, 256x256, 512x512, 1024x1024）

注意事项：
- 图标必须是 Texture2D 格式
- Unity会自动生成其他所需尺寸的图标
- 任务类型为 PreBuild，会在构建开始前执行";
        }

        public override async Task<BuildTaskResult> RunAsync(string output)
        {
            try
            {
                var currentTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                
                if (defaultIcon != null)
                {
                    // 获取该平台需要的图标尺寸
                    var iconSizes = PlayerSettings.GetIconSizesForTargetGroup(currentTargetGroup);
                    
                    if (iconSizes != null && iconSizes.Length > 0)
                    {
                        // 创建对应数量的图标数组，都使用同一个图标
                        var iconsToSet = new Texture2D[iconSizes.Length];
                        for (int i = 0; i < iconSizes.Length; i++)
                        {
                            iconsToSet[i] = defaultIcon;
                        }
                        
                        // 设置图标
                        PlayerSettings.SetIconsForTargetGroup(currentTargetGroup, iconsToSet);
                        Debug.Log($"{nameof(DefaultIconReplaceTask)}: 设置自定义图标 - {defaultIcon.name}，共 {iconSizes.Length} 个尺寸");
                    }
                    else
                    {
                        Debug.LogWarning($"{nameof(DefaultIconReplaceTask)}: 平台 {currentTargetGroup} 不需要图标或无法获取图标尺寸信息");
                        return BuildTaskResult.Successful(output);
                    }
                }
                else
                {
                    // 获取平台的图标尺寸，然后设置为空数组
                    var iconSizes = PlayerSettings.GetIconSizesForTargetGroup(currentTargetGroup);
                    if (iconSizes != null)
                    {
                        // 创建空的图标数组
                        var emptyIcons = new Texture2D[iconSizes.Length];
                        // 数组中的所有元素默认为 null，这样会使用Unity默认图标
                        PlayerSettings.SetIconsForTargetGroup(currentTargetGroup, emptyIcons);
                    }
                    
                    Debug.Log($"{nameof(DefaultIconReplaceTask)}: 恢复Unity默认图标");
                }
                
                // 刷新PlayerSettings和相关窗口
                RefreshPlayerSettings();
                
                Debug.Log($"{nameof(DefaultIconReplaceTask)}: 成功为平台 {currentTargetGroup} 设置图标");
                ReportResult(output, () => $"{nameof(DefaultIconReplaceTask)}: 图标替换完成 - ");

                return await Task.FromResult(BuildTaskResult.Successful(output));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{nameof(DefaultIconReplaceTask)}: 图标替换失败 - {e.Message}");
                return BuildTaskResult.Failed(output, e.Message);
            }
        }

        /// <summary>
        /// 更优雅地刷新PlayerSettings显示
        /// </summary>
        private void RefreshPlayerSettings()
        {
            // 另外尝试刷新所有打开的PlayerSettings窗口
            var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var window in allWindows)
            {
                if (window.GetType().Name == "ProjectSettingsWindow")
                {
                    Debug.Log($"{nameof(DefaultIconReplaceTask)}: 刷新 PlayerSettings 窗口");
                    window.Repaint();
                }
            }

        }

        public override bool Validate()
        {
            // 允许图标为空，所以总是返回true
            if (defaultIcon == null)
            {
                Debug.Log($"{nameof(DefaultIconReplaceTask)}: 将使用Unity默认图标");
            }
            else
            {
                Debug.Log($"{nameof(DefaultIconReplaceTask)}: 将使用自定义图标 - {defaultIcon.name}");
            }
            
            return true;
        }
    }
}