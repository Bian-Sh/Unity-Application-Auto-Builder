using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using zFramework.AppBuilder;
using zFramework.AppBuilder.Utils;
namespace zFramework.Extension
{
    /*
         // 以下时 Nsis 必要配置
        // 应用基础信息：名称；版本 ；注册表 App Paths；多语言
        // 公司、版权信息：公司名，主页，Branding Text (安装引导程序的左下角字样）
        //  应用安装时内含数据采集 与  快捷方式构建（需要自定义快捷方式参数）
        // 应用卸载时数据的删除（含快捷方式的移除）
        // 新增第三方应用以及调用参数
        // 约定：安装时会覆盖已有文件，卸载时会移除所有文件，所以，如有用户数据，请存储在非安装目录下！！！！
        // 约定：由于 Nsis 使用的路径是相对于 .nsi 文件的，为方便起见，.nsi 文件与要打包的文件夹放在同一目录下
     */
    /// <summary>
    ///  这个任务通过 makensis.exe + .nsi 文件生成 Window 系统下的 exe 安装程序！
    /// </summary>
    [CreateAssetMenu(fileName = "Nsis Installer Making Task", menuName = "Auto Builder/Task/Nsis Installer Making Task")]
    public class NsisInstallerMakingTask : BaseTask
    {
        [Header("makensis.exe 路径：")]
        public string exePath;

        [Header("Nsi 列表："), Tooltip("使用这种方式可以实现一个应用输出多个不同名称的安装包")]
        public List<NsiResolver> nsiResolvers;

        private void OnEnable()
        {
            taskType = TaskType.PostBuild;
            Description = "使用 makensis.exe 和 .nsi 文件生成 Windows 系统下的 exe 安装程序。Generate a Windows executable installer using makensis.exe and .nsi files.";
        }
        public override async Task<string> RunAsync(string output)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                throw new ArgumentNullException("makensis.exe 路径不可用，请检查！");
            }
            // 如果是文件，就使用其 Parent 文件夹
            if (File.Exists(output))
            {
                output = Path.GetDirectoryName(output);
            }
            if (string.IsNullOrEmpty(output) || !Directory.Exists(output))
            {
                throw new ArgumentNullException("output path is null or empty");
            }

            totalResolver = nsiResolvers.Count(v => v.enable && v.compileNsiFile);
            currentResolver = 1;
            try
            {
                foreach (var nsiResolver in nsiResolvers)
                {
                    if (!nsiResolver.enable)
                    {
                        continue;
                    }
                    var nsifile = nsiResolver.Process(output);

                    if (nsiResolver.compileNsiFile)
                    {
                        currentInstallerName = Path.GetFileName(nsiResolver.outputFileName.Replace("${PRODUCT_VERSION}", nsiResolver.appVersion));
                        progressid = Progress.Start($"({currentResolver}/{totalResolver})编译安装包 {currentInstallerName} ");

                        // 读取 output 目录下的所有文件相对路径并存放到 files 中，以便于编译时计算进度
                        files.Clear();
                        count = 0;
                        var root = new DirectoryInfo(output);
                        foreach (var file in root.GetFiles("*", SearchOption.AllDirectories))
                        {
                            files.Add(file.FullName.Replace(root.FullName, string.Empty).TrimStart('\\'));
                        }
                        // 调用 makensis.exe 进行编译
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = $"-V4 \"{nsifile}\"", // 使用 V4 log 等级
                            CreateNoWindow = true
                        };
                        var program = new Program(startInfo);

                        program.OnStandardOutputReceived += OnStandardOutputReceived;
                        program.OnStandardErrorReceived += (line) =>
                        {
                            Debug.LogError(line);
                        };
                        await program.StartAsync();
                        Progress.Remove(progressid);

                        currentResolver++;
                        if (program.ExitCode != 0)
                        {
                            throw new Exception($"Nsis 编译错误!");
                        }
                        else
                        {
                            Debug.Log($"{currentInstallerName} 编译完成!");
                        }
                    }

                    if (!nsiResolver.keepNsiFile)
                    {
                        File.Delete(nsifile);
                    }
                }
                return string.Empty; // 无需反馈
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //EditorUtility.ClearProgressBar();
                // ProgressBarWindow.ClearProgressBar();
            }
        }

        // 相对路径文件，当任务执行前记录
        private readonly List<string> files = new();
        private int count = 0;
        private int totalResolver, currentResolver, progressid;
        private string currentInstallerName;

        /// <summary>
        ///  Compile Nsi File log, 反馈编译进度
        /// </summary>
        /// <param name="obj"></param>
        private void OnStandardOutputReceived(string obj)
        {
            // 如果是文件处理,规则是以 “File: "”开头
            if (obj.StartsWith("File: \""))
            {
                var fileName = obj.Split('"')[1];
                count++;
                var step = (float)currentResolver / totalResolver;
                var progress = (float)count / files.Count * step;
                var globalProgress = Mathf.Clamp01((float)(currentResolver - 1) / totalResolver + progress);
                //EditorUtility.DisplayProgressBar($"({currentResolver}/{totalResolver})编译安装包 {currentInstallerName} ", $"({count }/{files.Count} )完成压缩：{fileName} ", globalProgress);
                //ProgressBarWindow.ShowProgressBar($"({currentResolver}/{totalResolver})编译安装包 {currentInstallerName} ", $"({count }/{files.Count} )完成压缩：{fileName} ", globalProgress);
                Progress.Report(progressid, (float)count / files.Count, $"({count}/{files.Count} )完成压缩：{fileName} ");
            }
        }

        public override bool Validate()
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                Debug.LogError("makensis.exe 路径不可用，请检查！");
                return false;
            }
            if (nsiResolvers == null || nsiResolvers.Count == 0 || nsiResolvers.Count(v => v.enable) == 0)
            {
                Debug.LogError("nsiResolvers 为空或均未激活，请检查！");
                return false;
            }
            // todo，遍历该对象及所有能够绘制到 Inspector 的字段所标注的 RequredAttribute 
            // 任意 RequredAttribute 检查不通过返回 false

            return true;
        }
    }
}