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

        [Header("保留 .ssp 文件？")]
        public bool keepSSP = false;

        [Header("需要被加密的 DLL 文件：")]
        public string[] dlls = new[] { "Assembly-CSharp.dll" };

        private void OnEnable()
        {
            Description = "通过这个任务使用 Virbox 加密服务商提供的服务加密应用程序！Use this task to encrypt your application with the service provided by Virbox!";
        }
        public override async Task<string> RunAsync(string output)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                throw new ArgumentNullException("Virbox 控制台程序路径不可用，请检查！");
            }
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentNullException("output path is null or empty");
            }

            //output = E:\Unity\Temp\AppLocation\AppTwo\AppTheSameNameIsOk.exe
            var applicationName = Path.GetFileNameWithoutExtension(output);

            var fileInfo = new FileInfo(output);
            var foldNameOrigin = fileInfo.Directory.Name;
            var root = fileInfo.Directory.Parent.FullName;
            var outputEncrypted = foldNameOrigin + "_Protected";
            var ssp = Path.Combine(root, $"{foldNameOrigin}.ssp");
            BuildSSPContent(exePath, ssp, dlls, outputEncrypted, applicationName);

            try
            {
                //多出来的2个分别是：UnityPlayer.dll、mono-2.0-bdwgc.dll，它们默认会被处理
                // 其他 asset 资产暂不做处理
                totalHandledFiles = dlls.Length + 2;
                count = 0;

                // run virbox encrypt
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    // 格式为 exepath originpath -u3d 
                    //virboxprotector_con "E:\Unity\Temp\AppLocation\AppTwo" -u3d
                    Arguments = $"\"{fileInfo.DirectoryName}\" -u3d",
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
                if (!keepSSP)
                {
                    File.Delete(ssp);
                }
                return $"{root}/{outputEncrypted}";
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

        // Report Progress
        private int totalHandledFiles = 0;
        private int count = 0;
        private void OnStandardOutputReceived(string obj)
        {
            Debug.Log(obj);

            if (obj.StartsWith("Protect assembly ", StringComparison.CurrentCultureIgnoreCase)
                || obj.StartsWith("protect file ", StringComparison.CurrentCultureIgnoreCase))
            {
                count++;
                EditorUtility.DisplayProgressBar("Virbox 加密中...", obj, (float)count / totalHandledFiles);
            }
        }

        private void BuildSSPContent(string exe, string ssp, string[] dlls, string saveto, string appName)
        {
            //获取 exe 的名称和版本信息 ：Virbox Protector 3 Trial (v3.4.0.20888)
            using Process process = new();
            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = "-?";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            var outputStr = process.StandardOutput.ReadToEnd();
            process.Close();
            var lines = outputStr.Split('\n');
            var line = lines[0];
            var arr = line.Split('(', ')');
            var exeName = arr[0].Trim();
            var exeVersion = arr[1].TrimStart('v');

            using var fs = new FileStream(ssp, FileMode.Create);
            using var sw = new StreamWriter(fs);
            var xmldoc = new System.Xml.XmlDocument();
            xmldoc.LoadXml(defaultssp);
            // 更新Virbox 名称及版本号
            xmldoc.SelectSingleNode("/ssprotect/base_info/app").InnerText = exeName;
            xmldoc.SelectSingleNode("/ssprotect/base_info/version").InnerText = exeVersion;

            //更新加密后的文件路径
            xmldoc.SelectSingleNode("/ssprotect/file/path").InnerText = saveto;
            // 装载要加密的 dll
            for (int i = 0; i < dlls.Length; i++)
            {
                var node = xmldoc.SelectSingleNode($"/ssprotect/assembly/file_{i + 1}");
                if (node == null)
                {
                    node = xmldoc.CreateElement($"file_{i + 1}");
                    xmldoc.SelectSingleNode("/ssprotect/assembly").AppendChild(node);
                }
                node.InnerText = $"{appName}_Data/Managed/{dlls[i]}";
            }
            xmldoc.Save(sw);
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
        const string defaultssp = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ssprotect>
	<base_info>
		<app>Virbox Protector 3 Trial</app>
		<version>3.4.0.20888</version>
	</base_info>
	<option>
		<anti_debugging>1</anti_debugging>
	</option>
	<file>
		<path>AppTwo_protected</path>
	</file>
	<sign />
	<function />
	<assembly>
	</assembly>
	<res_enc>
		<enable>0</enable>
		<file>
		</file>
	</res_enc>
</ssprotect>
";
    }
}