using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace zFramework.Extension
{
    [Serializable]
    public class NsiResolver
    {
        [Header("是否启用？")]
        public bool enable;
        [Header("App 信息：")]
        public string appName;
        public string appVersion;
        public string startMenuFolder;
        public string appInstallDir;
        public string outputFileName;
        public string installerOutputPath;
        [Header("版权信息：")]
        public string publisher;
        public string website;
        public string brandingText;
        [Header("内嵌组件：")]
        public Component[] components;
        [Header("多语言")]
        public string[] languages = new[] { "SimpChinese" };
        [Header("快捷方式：")]
        public Shotcut[] shotcuts;
        public string OutputFileLocation { get; private set; }


        public string Process(string output)
        {
            string exeEntry = Directory.GetFiles(output, "*.exe").Where(x => !x.StartsWith("UnityCrashHandler")).FirstOrDefault();
            if (string.IsNullOrEmpty(exeEntry))
            {
                throw new FileNotFoundException("Can not find exe file in output folder");
            }

            if (Directory.Exists(installerOutputPath) == false)
            {
                Directory.CreateDirectory(installerOutputPath);
            }

            appInstallDir = appInstallDir.Replace("/", "\\");
            var outputFileName = this.outputFileName.Replace("${PRODUCT_VERSION}", appVersion);

            OutputFileLocation = Path.Combine(installerOutputPath, outputFileName);
            // .nsi 文件存放在与输出目录同级目录下
            string nsiFilePath = Path.Combine(installerOutputPath, $"{outputFileName[..^4]}.nsi");

            string exeName = Path.GetFileName(exeEntry);
            var nsiBuilder = new StringBuilder(DefaultNsisScript);
            nsiBuilder.Replace("#Name#", appName)
                      .Replace("#Version#", appVersion)
                      .Replace("#ExeName#", exeName)
                      .Replace("#InstallDir#", appInstallDir)
                      .Replace("#OutputFileName#", OutputFileLocation)
                      .Replace("#Publisher#", publisher)
                      .Replace("#WebSite#", website)
                      .Replace("#BrandingText#", brandingText)
                      .Replace("#OriginDir#", output)
                      .Replace("#StartMenuDir#", startMenuFolder)
                      .Replace("#OutputDir#", installerOutputPath); // 生成的安装程序输出目录

            // 多语言
            string lang = string.Join("\n", languages.Select(x => $"!insertmacro MUI_LANGUAGE \"{x}\""));
            nsiBuilder.Replace("#Languate#", lang);

            // 构建快捷方式
            StringBuilder sb_add = new(), sb_remove = new();
            foreach (var shotcut in shotcuts)
            {
                string args = string.IsNullOrEmpty(shotcut.args) ? string.Empty : $" \"{shotcut.args}\"";
                sb_add.AppendLine($"CreateShortCut \"$SMPROGRAMS\\{startMenuFolder}\\{shotcut.name}.lnk\" \"$INSTDIR\\{exeName}\"{args}");
                sb_add.AppendLine($"CreateShortCut \"$DESKTOP\\{shotcut.name}.lnk\" \"$INSTDIR\\{exeName}\"{args}");
                sb_remove.AppendLine($"Delete \"$SMPROGRAMS\\{startMenuFolder}\\{shotcut.name}.lnk\"");
                sb_remove.AppendLine($"Delete \"$DESKTOP\\{shotcut.name}.lnk\"");
            }
            nsiBuilder.Replace("#AddedShotcut#", sb_add.ToString())
                      .Replace("#RemovedShotcut#", sb_remove.ToString());

            // 处理组件
            var idx = 2; //从 SEC02 开始
            StringBuilder sb = new();
            foreach (var component in components)
            {
                if (!component.enable)
                {
                    continue;
                }
                var SEC = $"SEC{(idx > 9 ? $"{idx}" : $"0{idx}")}";
                idx++;
                sb.AppendLine($"Section \"{component.sectionName}\" {SEC}");
                sb.AppendLine($"  SetOutPath \"$TEMP\"");
                sb.AppendLine($"  SetOverwrite ifnewer");
                sb.AppendLine($"  File \"{component.filePath}\"");
                sb.AppendLine($"  ExecWait '\"$TEMP\\{Path.GetFileName(component.filePath)}\" {component.args}' $0");
                sb.AppendLine($"  DetailPrint \"{Path.GetFileName(component.filePath)} $R0 return $0\"");
                sb.AppendLine($"  IntCmp $0 3 InstallSuccess InstallSuccess InstallError");
                sb.AppendLine($" InstallError:");
                sb.AppendLine($" ; TODO");
                sb.AppendLine($" Quit");
                sb.AppendLine($" InstallSuccess:");
                sb.AppendLine($" ; TODO");
                sb.AppendLine($"SectionEnd");
                sb.AppendLine();
            }
            nsiBuilder.Replace("#Components#", sb.ToString());
            File.WriteAllText(nsiFilePath, nsiBuilder.ToString(), Encoding.GetEncoding("GB2312"));
            return nsiFilePath;
        }

        readonly string DefaultNsisScript = @"; 该脚本使用 App Auto Builder 生成
; 安装程序初始定义常量
!define PRODUCT_NAME ""#Name#""
!define PRODUCT_VERSION ""#Version#""
!define PRODUCT_PUBLISHER ""#Publisher#""
!define PRODUCT_WEB_SITE ""#WebSite#""
!define PRODUCT_DIR_REGKEY ""Software\Microsoft\Windows\CurrentVersion\App Paths\#ExeName#""
!define PRODUCT_UNINST_KEY ""Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}""
!define PRODUCT_UNINST_ROOT_KEY ""HKLM""

SetCompressor lzma

; ------ MUI 现代界面定义 (1.67 版本以上兼容) ------
!include ""MUI.nsh""

; MUI 预定义常量
!define MUI_ABORTWARNING
!define MUI_ICON ""${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico""
!define MUI_UNICON ""${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico""

; 欢迎页面
!insertmacro MUI_PAGE_WELCOME
; 安装目录选择页面
!insertmacro MUI_PAGE_DIRECTORY
; 安装过程页面
!insertmacro MUI_PAGE_INSTFILES
; 安装完成页面
!define MUI_FINISHPAGE_RUN ""$INSTDIR\#ExeName#""
!insertmacro MUI_PAGE_FINISH

; 安装卸载过程页面
!insertmacro MUI_UNPAGE_INSTFILES

; 安装界面包含的语言设置
#Languate#

; 安装预释放文件
!insertmacro MUI_RESERVEFILE_INSTALLOPTIONS
; ------ MUI 现代界面定义结束 ------

Name ""${PRODUCT_NAME} v${PRODUCT_VERSION}""
!system 'mkdir ""#OutputDir#""'
OutFile ""#OutputFileName#""
InstallDir ""$PROGRAMFILES\#InstallDir#""
InstallDirRegKey HKLM ""${PRODUCT_UNINST_KEY}"" ""UninstallString""
ShowInstDetails show
ShowUnInstDetails show
BrandingText ""#BrandingText#""

; 程序以管理员权限运行
RequestExecutionLevel admin
 
Section ""runas""
 	;针对当前用户有效
	WriteRegStr HKCU ""SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" ""$INSTDIR\#ExeName#"" ""RUNASADMIN""
	;针对所有用户有效
	WriteRegStr HKEY_LOCAL_MACHINE ""SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" ""$INSTDIR\#ExeName#"" ""RUNASADMIN""
SectionEnd

Section ""MainSection"" SEC01
  SetOutPath ""$INSTDIR""
  SetOverwrite ifnewer
  File /r ""#OriginDir#\*.*""

; shotcut 创建
  CreateDirectory ""$SMPROGRAMS\#StartMenuDir#"" 
  #AddedShotcut#
SectionEnd

; 组件
#Components#

Section -AdditionalIcons
  WriteIniStr ""$INSTDIR\${PRODUCT_NAME}.url"" ""InternetShortcut"" ""URL"" ""${PRODUCT_WEB_SITE}""
  CreateShortCut ""$SMPROGRAMS\#StartMenuDir#\Website.lnk"" ""$INSTDIR\${PRODUCT_NAME}.url""
  CreateShortCut ""$SMPROGRAMS\#StartMenuDir#\Uninstall.lnk"" ""$INSTDIR\uninst.exe""
SectionEnd

Section -Post
  WriteUninstaller ""$INSTDIR\uninst.exe""
  WriteRegStr HKLM ""${PRODUCT_DIR_REGKEY}"" """" ""$INSTDIR\#ExeName#""
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""DisplayName"" ""$(^Name)""
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""UninstallString"" ""$INSTDIR\uninst.exe""
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""DisplayIcon"" ""$INSTDIR\#ExeName#""
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""DisplayVersion"" ""${PRODUCT_VERSION}""
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""URLInfoAbout"" ""${PRODUCT_WEB_SITE}""
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""Publisher"" ""${PRODUCT_PUBLISHER}""
SectionEnd

/******************************
 *  以下是安装程序的卸载部分  *
 ******************************/

Section Uninstall
  Delete ""$INSTDIR\${PRODUCT_NAME}.url""
  Delete ""$INSTDIR\uninst.exe""
  Delete ""$SMPROGRAMS\#StartMenuDir#\Uninstall.lnk""
  Delete ""$SMPROGRAMS\#StartMenuDir#\Website.lnk""

 #RemovedShotcut#

  RMDir ""$SMPROGRAMS\#StartMenuDir#""
  RMDir /r ""$INSTDIR""

  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}""
  DeleteRegKey HKLM ""${PRODUCT_DIR_REGKEY}""
  SetAutoClose true
SectionEnd

#-- 根据 NSIS 脚本编辑规则，所有 Function 区段必须放置在 Section 区段之后编写，以避免安装程序出现未可预知的问题。--#

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 ""您确实要完全移除 $(^Name) ，及其所有的组件？"" IDYES +2
  Abort
FunctionEnd

Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK ""$(^Name) 已成功地从您的计算机移除。""
FunctionEnd
";

        #region Assistance Type
        /// <summary>
        ///  第三方组件默认释放到 Temp 目录下，并使用 args 参数进行安装
        /// </summary>
        [Serializable]
        public class Component
        {
            /// <summary>
            ///  启用与否
            /// </summary>
            public bool enable;
            /// <summary>
            ///  章节/分段名称
            /// </summary>
            public string sectionName;
            /// <summary>
            ///  文件路径
            /// </summary>
            public string filePath;
            /// <summary>
            ///  安装参数，如果不想看见安装界面，请指定对应静默安装参数
            /// </summary>
            public string args;
        }

        [Serializable]
        public class Shotcut
        {
            public string name;
            public string args; // 根据上下文自动获取、 自动拼接到目标程序后面
        }
        #endregion
    }
}
