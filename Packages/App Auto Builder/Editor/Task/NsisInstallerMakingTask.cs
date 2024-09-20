using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentNullException("output path is null or empty");
            }

            foreach (var nsiResolver in nsiResolvers)
            {
                var nsifile = nsiResolver.Process(output);

                if (nsiResolver.compileNsiFile)
                {
                    // 调用 makensis.exe 进行编译
                    var startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.FileName = exePath;
                    // 使用 V4 log 等级
                    startInfo.Arguments = $"-V4 \"{nsifile}\"";
                    startInfo.UseShellExecute = false;
                     startInfo.CreateNoWindow = true;
                    var program = new Program(startInfo);

                    program.OnStandardOutputReceived += OnStandardOutputReceived; 
                    program.OnStandardErrorReceived += (line) =>
                    {
                        Debug.LogError(line);
                    };
                    await program.StartAsync();
                    if (program.ExitCode != 0)
                    {
                        throw new Exception($"Nsis 编译错误!");
                    }
                    else
                    {
                        Debug.Log($"Nsis 编译完成!");
                    }
                }

                if (!nsiResolver.keepNsiFile)
                {
                    File.Delete(nsifile);
                }
            }
            return string.Empty; // 无需反馈
        }

        // 当前下沉到的目录，当Log出现 “File: Descending to:” 字样时记录
        private string location;
        // 相对路径文件，当任务执行前记录
        private readonly HashSet<string> files = new();

        private void OnStandardOutputReceived(string obj)
        {
            //Progress Bar Report Here





        }
    }
}