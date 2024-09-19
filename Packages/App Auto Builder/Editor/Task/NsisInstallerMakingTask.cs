using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
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
namespace zFramework.AppBuilder
{
    /// <summary>
    ///  这个任务通过 makensis.exe + .nsi 文件生成 Window 系统下的 exe 安装程序！
    /// </summary>
    [CreateAssetMenu(fileName = "Nsis Installer Making Task", menuName = "Auto Builder/Task/Nsis Installer Making Task")]
    public class NsisInstallerMakingTask : BaseTask
    {
        [Header("makensis.exe 路径：")]
        public string exePath;
        [Header("App 信息：")]
        public string appName;
        public string appVersion;
        public string startMenuFolder;
        public string appInstallDir; // 安装目录拼接 ${PRODUCT_VERSION} 可以实现版本号目录
        public string outputFileName; // 应用名+v版本号+setup.exe ，全小写，例如：myapp-v1.0-setup.exe
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
        [Header("保留 .nsi 脚本？")]
        public bool keepNsiFile = false;
        [Header("编译 .nsi 脚本？")]
        public bool compileNsiFile = true;


        private void OnEnable()
        {
            taskType = TaskType.PostBuild;
            Description = "使用 makensis.exe 和 .nsi 文件生成 Windows 系统下的 exe 安装程序。Generate a Windows executable installer using makensis.exe and .nsi files.";
        }
        public override string Run(string output)
        {
            Debug.Log($"Run {nameof(NsisInstallerMakingTask)} ,output = {output} !");
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                throw new ArgumentNullException("makensis.exe 路径不可用，请检查！");
            }
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentNullException("output path is null or empty");
            }
            // 传递过来的是准备构建安装包的文件夹路径，需要在这个目录的同级目录下生成 .nsi 文件
            // 然后调用 makensis.exe 进行编译，生成安装程序也在此目录同级目录下
            string nsiFilePath = Path.Combine(output, $"../{appName}-v{appVersion}-setup.nsi");
            string exeEntry = Directory.GetFiles(output, "*.exe").Where(x => !x.StartsWith("UnityCrashHandler")).FirstOrDefault();
            if (string.IsNullOrEmpty(exeEntry))
            {
                throw new FileNotFoundException("Can not find exe file in output folder");
            }
            appInstallDir = appInstallDir.Replace("/", "\\");
            outputFileName = outputFileName.Replace("${PRODUCT_VERSION}", appVersion);//NSIS 脚本不支持 ${PRODUCT_VERSION} 这种写法,所以替他处理了

            string exeName = Path.GetFileName(exeEntry);
            string originDir = Path.GetFileName(output);
            var nsiBuilder = new StringBuilder(defaultNsisScript);
            nsiBuilder.Replace("#Name#", appName)
                      .Replace("#Version#", appVersion)
                      .Replace("#ExeName#", exeName)
                      .Replace("#InstallDir#", appInstallDir)
                      .Replace("#OutputFileName#", outputFileName)
                      .Replace("#Publisher#", publisher)
                      .Replace("#WebSite#", website)
                      .Replace("#BrandingText#", brandingText)
                      .Replace("#OriginDir#", originDir)
                      .Replace("#StartMenuDir#", startMenuFolder);

            // 多语言
            string lang = string.Join("\n", languages.Select(x => $"!insertmacro MUI_LANGUAGE \"{x}\""));
            nsiBuilder.Replace("#Languate#", lang);
            // 构建快捷方式
            StringBuilder sb_add = new(), sb_remove = new();
            foreach (var shotcut in shotcuts)
            {
                string args = string.IsNullOrEmpty(shotcut.args) ? string.Empty : $" \"{shotcut.args}\"";

                // add start menu shotcut at install section
                // CreateShortCut ""$SMPROGRAMS\#StartMenuDir#\#Name#.lnk"" ""$INSTDIR\#ExeName#""
                sb_add.AppendLine($"CreateShortCut \"$SMPROGRAMS\\{startMenuFolder}\\{shotcut.name}.lnk\" \"$INSTDIR\\{exeName}\"{args}");
                // add  desktop shotcut at install section
                // CreateShortCut ""$DESKTOP\#Name#.lnk"" ""$INSTDIR\#ExeName#""
                sb_add.AppendLine($"CreateShortCut \"$DESKTOP\\{shotcut.name}.lnk\" \"$INSTDIR\\{exeName}\"{args}");
                // delete startmenu shotcut at uninstall section
                //; Delete ""$SMPROGRAMS\#StartMenuDir#\#Name#.lnk""
                sb_remove.AppendLine($"Delete \"$SMPROGRAMS\\{startMenuFolder}\\{shotcut.name}.lnk\"");
                // delete desktop shotcut at uninstall section
                //;  Delete ""$DESKTOP\#Name#.lnk""
                sb_remove.AppendLine($"Delete \"$DESKTOP\\{shotcut.name}.lnk\"");
            }
            nsiBuilder.Replace("#AddedShotcut#", sb_add.ToString())
                      .Replace("#RemovedShotcut#", sb_remove.ToString());

            // 处理组件
            var idx = 2; //从 SEC02 开始
            StringBuilder sb = new();
            foreach (var component in components)
            {
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

            // 使用 GB2312 编码保存
            File.WriteAllText(nsiFilePath, nsiBuilder.ToString(), Encoding.GetEncoding("GB2312"));

            // 调用 makensis.exe 进行编译, V4 log 等级 args =  $"-V4 \"{nsiFilePath}\""
            if (compileNsiFile) 
            {

            }


            if (!keepNsiFile)
            {
                File.Delete(nsiFilePath);
            }

            return string.Empty; // 无需反馈
        }








        /// <summary>
        ///  第三方组件默认释放到 Temp 目录下，并使用 args 参数进行安装
        /// </summary>
        [Serializable]
        public class Component
        {
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

        // 一些自定义占位符：
        // App 信息
        // #Name#  、#Version# 、#ExeName#、#InstallDir#
        // #StartMenuDir# 、#OutputFileName#、#OriginDir#
        // #Language# 支持多语音，使用英文字符 ; 分割即可
        // #AddedShotcut# 在这里处理快捷方式的添加，包括开始菜单、桌面
        // #RemovedShotcut# 在这里处理快捷方式的移除，包括开始菜单、桌面
        // 版权信息
        // #Publisher#  、#WebSite# 、#BrandingText#
        // 组件信息
        // #Components# 



        const string defaultNsisScript = @"; 该脚本使用 App Auto Builder 生成
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
;!insertmacro MUI_LANGUAGE ""SimpChinese""
; 使用 #Language# 占位符，支持多语言，使用英文字符 ; 分隔，
; 解析时分割出 多语言描述字符串，并拼接出来形如 !insertmacro MUI_LANGUAGE ""SimpChinese"" 语句替换此占位符
; 每多一个语言，就拼接一个 !insertmacro MUI_LANGUAGE ""your lang"" 并换行保存
#Languate#

; 安装预释放文件
!insertmacro MUI_RESERVEFILE_INSTALLOPTIONS
; ------ MUI 现代界面定义结束 ------

Name ""${PRODUCT_NAME} ${PRODUCT_VERSION}""
OutFile ""#OutputFileName#""
InstallDir ""$PROGRAMFILES\#InstallDir#""
InstallDirRegKey HKLM ""${PRODUCT_UNINST_KEY}"" ""UninstallString""
ShowInstDetails show
ShowUnInstDetails show
BrandingText ""#BrandingText#""

Section ""MainSection"" SEC01
  SetOutPath ""$INSTDIR""
  SetOverwrite ifnewer
  File /r ""#OriginDir#\*.*""

; todo : 这里存在应用相同启动参数不同的多个 shotcut 创建情景
CreateDirectory ""$SMPROGRAMS\#StartMenuDir#"" 
;  CreateShortCut ""$SMPROGRAMS\#StartMenuDir#\#Name#.lnk"" ""$INSTDIR\#ExeName#""
;  CreateShortCut ""$DESKTOP\#Name#.lnk"" ""$INSTDIR\#ExeName#""
#AddedShotcut#

SectionEnd

; 组件
/*
在这里处理组件的安装，请按照以下格式添加组件
其中 sense_shield_installer_pub_2.5.0.59543.exe为第三方组件的安装程序
其中 /S /senseshield_hide_taskbar_icon /not_create_desktop_shortcuts 为第三方组件的静默安装参数，仅供参考
默认释放到 $TEMP 目录下
监听 InstallSuccess 与 InstallError 事件但是可不用实现

Section ""protect"" SEC02
  SetOutPath ""$TEMP""
  SetOverwrite ifnewer
  File ""..\..\sense_shield_installer_pub_2.5.0.59543.exe""
  ExecWait '""$TEMP\sense_shield_installer_pub_2.5.0.59543.exe"" /S /senseshield_hide_taskbar_icon /not_create_desktop_shortcuts' $0
  DetailPrint ""sense_shield_installer_pub_2.5.0.59543.exe $R0 return $0""
  IntCmp $0 3 InstallSuccess InstallSuccess InstallError
 InstallError:
 ; TODO
 Quit
 InstallSuccess:
 ; TODO
SectionEnd
*/
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

;  Delete ""$DESKTOP\#Name#.lnk""
;  Delete ""$SMPROGRAMS\#StartMenuDir#\#Name#.lnk""
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
    }
}