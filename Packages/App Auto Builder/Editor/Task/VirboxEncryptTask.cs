using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using zFramework.AppBuilder.Utils;
using Debug = UnityEngine.Debug;
namespace zFramework.AppBuilder
{
    //add a task to run a process, it is a simple task to run a process,without io redirect
    //添加一个任务来运行一个进程, 这是一个简单的任务来运行一个进程，不带io重定向
    [CreateAssetMenu(fileName = "Virbox Encrypt Task", menuName = "Auto Builder/Task/Virbox Encrypt Task")]
    public class VirboxEncryptTask : BaseTask
    {
        [Header("virbox 控制台程序路径：")]
        public string exePath;

        [Header("需要被加密的 DLL 文件：")]
        public string[] dlls = new[] { "Assembly-CSharp.dll" };

        private void OnEnable()
        {
            taskType = TaskType.PostBuild;
            Description = "通过这个任务使用 Virbox 加密服务商提供的服务加密应用程序！Use this task to encrypt your application with the service provided by Virbox!";
        }
        
        //output = E:\Unity\Temp\AppLocation\AppTwo\AppTheSameNameIsOk.exe
        public override async Task<string> RunAsync(string output)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                throw new ArgumentNullException("Virbox 控制台程序路径不可用，请检查！");
            }
            if (string.IsNullOrEmpty(output) || !File.Exists(output))
            {
                throw new ArgumentNullException("exe file is null or empty or file is not exists!");
            }
            var applicationName = Path.GetFileNameWithoutExtension(output);

            var fileInfo = new FileInfo(output);
            var foldNameOrigin = fileInfo.Directory.Name;
            var root = fileInfo.Directory.Parent.FullName;
            var log = Path.Combine(root, $"{foldNameOrigin}.log");

            try
            {
                //多出来的2个分别是：UnityPlayer.dll、mono-2.0-bdwgc.dll，它们默认会被处理
                // 其他 asset 资产暂不做处理
                EditorUtility.DisplayProgressBar("Virbox 加密中，请等待...", "", 0.8F);

                // 装载要加密的 dll
                var asmArgs = "-asm \"";
                for (int i = 0; i < dlls.Length; i++)
                {
                    asmArgs += $"{applicationName}_Data/Managed/{dlls[i]};";
                }
                asmArgs = asmArgs.TrimEnd(';') + "\"";
                // 命令行参数：inputDir -u3d --res-enc=1 -asm "Assembly-CSharp.dll;Assembly-CSharp-firstpass.dll" 
                // 如果不指定 outputDir，则默认为 inputDir 同级目录, 且文件名后面会加上 _protected
                var combinedArgs = $"\"{fileInfo.DirectoryName}\" -u3d --res-enc=1 {asmArgs}";

                File.WriteAllText(log, combinedArgs);

                // run virbox encrypt
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = combinedArgs,
                    CreateNoWindow = true
                };
                var program = new Program(startInfo);
                program.OnStandardErrorReceived += (errorStr) =>
                {
                    if (!string.IsNullOrEmpty(errorStr))
                    {
                        var message = errorStr.Contains("A0000011") ? "请先登录 Virbox 账号!" : errorStr;
                        EditorUtility.DisplayDialog("Virbox 加密错误", message, "确定");
                        throw new Exception(errorStr);
                    }
                };
                program.OnStandardOutputReceived += OnStandardOutputReceived;
                await program.StartAsync();
                Debug.Log("Virbox 加密完成！");
                return $"{root}/{foldNameOrigin}_protected";
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void OnStandardOutputReceived(string obj)
        {
            Debug.Log(obj);
        }

        public override bool Validate()
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                Debug.LogError("Virbox 控制台程序路径不可用，请检查！");
                return false;
            }
            return true;
        }
    }
}