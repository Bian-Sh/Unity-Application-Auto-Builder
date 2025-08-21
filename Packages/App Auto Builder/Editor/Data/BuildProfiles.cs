using System;
using System.Collections.Generic;
using UnityEngine;
namespace zFramework.AppBuilder
{
    [Serializable]
    public class BuildProfiles : ISerializationCallbackReceiver
    {
        [Header("应用名称：")]
        public string productName;
        [Header("是否出包：")]
        public bool isBuild;
        [Header("打包平台")]
        public Platform platform;
        [Header("保存路径（Sub）"), ShowInExplorer]
        public string saveLocation;
        [Header("软件版本：（形如：1.0.0）")]
        public string productVersion;
        [Header("构建可选项："), EnumFlags]
        public BuildOptionsLit buildOptions;
        [Header("场景列表：不勾选不打包，注意排序")]
        public List<SceneInfo> scenes;
        [Header("用户自定义任务")]
        public List<TaskInfo> customTask;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // 序列化之前，休整那些首尾一定不可以存在空格的数据
            productName = productName?.Trim();
            saveLocation = saveLocation?.Trim();
            productVersion = productVersion?.Trim();
        }
    }
}