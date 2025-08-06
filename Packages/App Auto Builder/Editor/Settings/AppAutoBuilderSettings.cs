using UnityEngine;
using System;

namespace zFramework.AppBuilder
{
    [Serializable]
    public class AppAutoBuilderSettings : ScriptableObject
    {
        [Tooltip("仅加壳 virbox ")]
        public string virboxExePath;
        [Tooltip("加壳加授权 virbox ，带(LM)字样, 64位系统请选择路径中带 bin64 字样的！")]
        public string virboxLMExePath;
        [Tooltip("开发者 id, 更多信息请访问：https://developer-new.lm.virbox.com/#/developerInfo")]
        public string devloperId; 
        [SerializeField]
        private string virboxPin; 
        public string VirboxPin
        {
            get
            {
                return EncryptPin(virboxPin);
            }
            set
            {
                virboxPin = EncryptPin(value);
            }
        }
        [Header("Virbox API 密码"),Tooltip("请先使用图形工具完成 API 密码的配置，并测试加密，在生成 .ssp 文件“license/password” 属性节点中找到它！ ")]
        public string virboxApiPsw;

        [Tooltip("NSIS 打包工具安装路径")]
        public string nsisExePath;
        [Tooltip("为方便调试，你可以选择是否将 NSIS 文件保留")]
        public bool shouldKeepNsisFile;

        private string EncryptPin(string psw)
        {
            //  加密 virboxPsw
            if (!string.IsNullOrEmpty(psw))
            {
                var chars = psw.ToCharArray();
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)(chars[i] ^ 0x5A);
                }
                psw = new string(chars);
            }
            return psw;
        }
    }
}
