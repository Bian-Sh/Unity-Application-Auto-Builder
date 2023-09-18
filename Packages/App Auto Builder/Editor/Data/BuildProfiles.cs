using System;
using System.Collections.Generic;
using UnityEngine;
namespace zFramework.Extension
{
    [Serializable]
    public class BuildProfiles
    {
        [Header("应用名称：")]
        public string productName;
        [Header("存放文件夹：")]
        public string saveLocation;
        [Header("软件版本：（形如：1.0.0）")]
        public string productVersion;
        [Header("是否出包：")]
        public bool isBuild;
        [Header("构建可选项："), EnumFlags]
		public BuildOptionsLit buildOptions;
		[Header("场景列表：不勾选不打包，注意排序")]
        public List<SceneInfo> scenes;
        [Header("用户自定义任务")]
        public List<TaskInfo> customTask;

    }
}