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
            // run virbox encrypt
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = exePath;
            // 格式为 exepath originpath -u3d 
            //virboxprotector_con "E:\Unity\Temp\AppLocation\AppTwo" -u3d
            startInfo.Arguments = $"\"{fileInfo.DirectoryName}\" -u3d";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
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
            program.OnStandardOutputReceived += (line) =>
            {
                Debug.Log(line);
            };

            await program.StartAsync();
            if (!keepSSP)
            {
                File.Delete(ssp);
            }
            return $"{root}/{outputEncrypted}";
        }

        private void BuildSSPContent(string exe, string ssp, string[] dlls, string saveto, string appName)
        {
            var exeName = string.Empty;
            var exeVersion = string.Empty;
            //获取 exe 的名称和版本信息 ：Virbox Protector 3 Trial (v3.4.0.20888)
            /*
             C:\Program Files\senseshield\Virbox Protector 3 Trial\bin>virboxprotector_con -?
    Virbox Protector 3 Trial (v3.4.0.20888)
    Copyright(c) SenseShield Technology Co., Ltd.  All rights reserved.

    usage:
    virboxprotector_con <input_path> <options ...> [-o <output_path>]
        --help={native|dotnet|apk|aab|aar|app|ipa|u3d|java-bce|java-vme|h5|strip|
                u3dres|mulpkg|ilmerge|obj|archive|objmerge|global|license}


        -java <dir_path>          : protect java application (java-bce)
        -h5 <input_path>          : protect html5 application (.js).

        -bind                     : bind license.(activating the product)
        -unbind                   : unbind license.
            */

            using var process = new System.Diagnostics.Process();
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
            exeName = arr[0].Trim();
            exeVersion = arr[1].TrimStart('v');

            // log exe name and version
            Debug.Log($"Virbox Protector: {exeName} {exeVersion}");

            using var fs = new FileStream(ssp, FileMode.Create);
            using var sw = new StreamWriter(fs);
            var xmldoc = new System.Xml.XmlDocument();
            xmldoc.LoadXml(defaultssp);
            // 将 app 赋值 <app>Virbox Protector 3 Trial</app>
            xmldoc.SelectSingleNode("/ssprotect/base_info/app").InnerText = exeName;
            // 将 version 赋值 <version>3.4.0.20888</version>
            xmldoc.SelectSingleNode("/ssprotect/base_info/version").InnerText = exeVersion;

            //将 saveto 赋值 <file><path> save to </path></file> 
            xmldoc.SelectSingleNode("/ssprotect/file/path").InnerText = saveto;
            // 将 dlls 赋值 <assembly><file_x> dlls </file_x></assembly> , x 从 2 开始,默认保留 Assembly-CSharp.dll
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
            // show in explorer 
            EditorUtility.RevealInFinder(ssp);
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