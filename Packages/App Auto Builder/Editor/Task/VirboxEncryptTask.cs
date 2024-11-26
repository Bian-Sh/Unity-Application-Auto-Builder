using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using zFramework.AppBuilder.Utils;
using Debug = UnityEngine.Debug;
using static zFramework.AppBuilder.AppAutoBuilderSettingProvider;
using System.Xml;
namespace zFramework.AppBuilder
{
    //add a task to run a process, it is a simple task to run a process,without io redirect
    //添加一个任务来运行一个进程, 这是一个简单的任务来运行一个进程，不带io重定向
    [CreateAssetMenu(fileName = "Virbox Encrypt Task", menuName = "Auto Builder/Task/Virbox Encrypt Task")]
    public class VirboxEncryptTask : BaseTask
    {
        [Header("需要被加密的 DLL 文件：")]
        public string[] dlls = new[] { "Assembly-CSharp.dll" };
        [Header("产品 ID"), Tooltip("如果仅加壳请留空，否则将调用授权版本的 Virbox")]
        public string productId;
        private void OnEnable()
        {
            taskType = TaskType.PostBuild;
            Description = "通过这个任务使用 Virbox 加密服务商提供的服务加密应用程序！Use this task to encrypt your application with the service provided by Virbox!";
        }

        //output = E:\Unity\Temp\AppLocation\AppTwo\AppTheSameNameIsOk.exe
        /// <summary>
        ///  执行 Virbox 加密任务
        /// </summary>
        /// <param name="args"> 打包出来的 App 文件路径 </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override async Task<string> RunAsync(string args)
        {
            var exePath = string.IsNullOrEmpty(productId) ? Settings.virboxExePath : Settings.virboxLMExePath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                // Show Settings
                ShowSettings();
                throw new ArgumentNullException($"Virbox 控制台程序路径不可用，请检查！\n{exePath}");
            }
            if (string.IsNullOrEmpty(args) || !File.Exists(args))
            {
                throw new ArgumentNullException("exe file is null or empty or file is not exists!");
            }
            var applicationName = Path.GetFileNameWithoutExtension(args);

            var fileInfo = new FileInfo(args);
            var foldNameOrigin = fileInfo.Directory.Name;
            var root = fileInfo.Directory.Parent.FullName;
            var output = $"{root}/{foldNameOrigin}_protected";

            try
            {
                //多出来的2个分别是：UnityPlayer.dll、mono-2.0-bdwgc.dll，它们默认会被处理
                // 其他 asset 资产暂不做处理
                EditorUtility.DisplayProgressBar("Virbox 加密中，请等待...", "", 0.8F);

                // 删除 .ssp 文件，如果有的话
                var sspFiles = Path.Combine(root, $"{foldNameOrigin}.ssp");
                if (File.Exists(sspFiles))
                {
                    File.Delete(sspFiles);
                }

                var combinedArgs = string.Empty;
                if (string.IsNullOrEmpty(productId))
                {
                    // 装载要加密的 dll
                    var asmArgs = "-asm \"";
                    for (int i = 0; i < dlls.Length; i++)
                    {
                        asmArgs += $"{applicationName}_Data/Managed/{dlls[i]};";
                    }
                    asmArgs = asmArgs.TrimEnd(';') + "\"";
                    // 命令行参数：inputDir -u3d --res-enc=1 -asm "Assembly-CSharp.dll;Assembly-CSharp-firstpass.dll" 
                    // 如果不指定 outputDir，则默认为 inputDir 同级目录, 且文件名后面会加上 _protected
                    combinedArgs = $"\"{fileInfo.DirectoryName}\" -u3d --res-enc=1 {asmArgs}";
                }
                else
                {
                    ModifyVirboxConfiguration(exePath,sspFiles, output);
                    combinedArgs = $"\"{root}\" -c local -p {Settings.VirboxPsw} -u3d -o \"{output}\"";
                }

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
                        if (errorStr.Contains("A0000011"))
                        {
                            errorStr = "请先登录 Virbox 账号!";
                        }
                        else if (errorStr.Contains("B000000D"))
                        {
                            errorStr = "请先插入 Virbox 控制锁！";
                        }
                        EditorUtility.DisplayDialog("Virbox 加密错误", errorStr, "确定");
                        throw new Exception(errorStr);
                    }
                };
                program.OnStandardOutputReceived += OnStandardOutputReceived;
                await program.StartAsync();
                Debug.Log("Virbox 加密任务执行完毕，请检查是否有报错！");
                ReportResult(output, () => "Virbox 加密任务输出目录：");
                return output;
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

        private void ModifyVirboxConfiguration(string virboxPath,string configPath, string output)
        {
            var config = configPath;
            XmlDocument xml = new XmlDocument();
            // 使用 xml 读取 xml 配置，修改产品 id后写入到 configPath
            xml.LoadXml(this.xml);
            var root = xml.DocumentElement;

            // 修改 develop id  为开发者 id, id = 0800000000001EA0
            var developer = root.SelectSingleNode("developer");
            var developer_id = developer.SelectSingleNode("id");
            developer_id.InnerText = Settings.devloperId.Trim();

            // 修改 license - id 为产品 id
            var license = root.SelectSingleNode("license");
            var id = license.SelectSingleNode("id");
            id.InnerText = productId.Trim();

            // 修改 senseshield - path 为 virboxPath “\sdk” 加前半部分的路径
            var senseshield = root.SelectSingleNode("senseshield");
            var path = senseshield.SelectSingleNode("path");
            var temp = virboxPath.Replace('/', '\\');
            path.InnerText = temp.Substring(0, temp.IndexOf("\\sdk") + 4);

            // 修改 file - path 为 output 文件夹名称
            var file = root.SelectSingleNode("file");
            var file_path = file.SelectSingleNode("path");
            file_path.InnerText = Path.GetFileName(output);

            // 为 assembly 插入文件，文件名称来自于 window.dllNeedEncrypt
            var assembly = root.SelectSingleNode("assembly");
            /*
                <assembly>
                    <file_1>data/Managed/com.realis-e.rovonga.protocol - 副本.dll</file_1>
                    <file_2>data/Managed/com.realis-e.rovonga.protocol.dll</file_2>
                </assembly>
             */
            for (var i = 0; i < dlls.Length; i++)
            {
                var file_i = assembly.SelectSingleNode($"file_{i + 1}");
                if (file_i == null)
                {
                    file_i = xml.CreateElement($"file_{i + 1}");
                    assembly.AppendChild(file_i);
                }
                file_i.InnerText = dlls[i];
            }
            xml.Save(config);
        }

        private void OnStandardOutputReceived(string obj)
        {
            //Debug.Log(obj);
        }

        public override bool Validate()
        {
            var exePath = Settings.virboxExePath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                Debug.LogError("Virbox 控制台程序路径不可用，请检查！");
                return false;
            }
            return true;
        }

        #region ssp xml content
        private readonly string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ssprotect>
	<base_info>
		<app>Virbox Protector 3 (LM)</app>
		<version>3.3.1.20115</version>
	</base_info>
	<developer>
		<id>0800000000001EA0</id>
	</developer>
	<senseshield>
		<path>C:\Program Files (x86)\senseshield\sdk</path>
	</senseshield>
	<runtime>
		<debug>0</debug>
		<outside>0</outside>
	</runtime>
	<license>
		<mode>1</mode>
		<id>202402231</id>
		<password>:N==MN:96I6==6;L?&gt;66K=M:9=?9:6MJ</password>
		<serial></serial>
	</license>
	<file>
		<path>Unity3D_protected</path>
	</file>
	<option>
		<anti_debugging>1</anti_debugging>
		<interval>60</interval>
		<unplug>0</unplug>
	</option>
	<sign />
	<message language=""1"" show=""1"" freeze=""1"" showloginbutton=""1"" timeout=""0"" reminder_interval=""0"">
		<title>Virbox Protector</title>
		<cloud>
			<SENSE66000101>登录的账号或者密码错误</SENSE66000101>
			<SENSE66000102>参数错误</SENSE66000102>
			<SENSE66000103>用户登录服务未启动</SENSE66000103>
			<SENSE66000104>登录超时</SENSE66000104>
			<SENSE66000105>获取用户信息失败</SENSE66000105>
			<SENSE66000106>未知错误</SENSE66000106>
		</cloud>
		<define>
			<SENSE13000020>错误码{ERROR}:找不到许可，{ENTER}运行此程序需要读取{LICNO}号许可，但是在此计算机上找不到许可，请插入用户锁或者是登录云账户，然后点击“重试”按钮继续。</SENSE13000020>
			<SENSE22000011>错误码{ERROR}:许可尚不可用，{ENTER}运行此程序需要读取{LICNO}号许可，此计算机上该许可尚不可用，程序无法继续运行。请联系销售商更新许可。</SENSE22000011>
			<SENSE22000012>错误码{ERROR}:此程序绑定的许可已到期，为了避免影响您的使用，请尽快联系销售商更新许可。{ENTER}当前绑定的许可号：{LICNO}号</SENSE22000012>
			<SENSE22000015>错误码{ERROR}:许可达到最大并发数，{ENTER}运行此程序需要读取{LICNO}号许可，此计算机上该许可已经达到最大并发计数，程序无法继续运行。请联系销售商更新许可。</SENSE22000015>
			<SENSE66000002>10</SENSE66000002>
			<SENSE66000003>10</SENSE66000003>
			<SENSE66000004>剩余使用次数：{COUNT}次{ENTER}</SENSE66000004>
			<SENSE66000005>剩余使用时间：{DAY}天{ENTER}</SENSE66000005>
			<SENSE66000007>客户您好，您的加密锁(S/N:{WSN})支持丢锁补锁功能，还有{DAY}天过期，请及时通过Virbox用户工具激活该加密锁，否则将影响您软件的使用。</SENSE66000007>
		</define>
		<login>
			<SENSE70000001>云账号登录</SENSE70000001>
			<SENSE70000002>用户名</SENSE70000002>
			<SENSE70000003>密码</SENSE70000003>
			<SENSE70000006>云账号登录</SENSE70000006>
			<SENSE70000007>取消</SENSE70000007>
			<SENSE70000008>注册</SENSE70000008>
			<SENSE70000009>https://auth.senseyun.com/register.jsp</SENSE70000009>
			<SENSE70000010>忘记密码</SENSE70000010>
			<SENSE70000011>https://auth.senseyun.com/forgot.jsp</SENSE70000011>
			<SENSE70000012>      </SENSE70000012>
			<SENSE70000013>      </SENSE70000013>
		</login>
		<error>
			<SENSE30000003>许可未登录或已超时</SENSE30000003>
			<SENSE2400000d>检测到许可会话失效，请插入加密锁或者排查网络锁连接故障</SENSE2400000d>
		</error>
	</message>
	<function />
	<assembly>
		<file_1>data/Managed/Realis.dll</file_1>
	</assembly>
	<res_enc>
		<enable>0</enable>
		<file />
	</res_enc>
</ssprotect>";
        #endregion
    }
}