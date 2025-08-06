using System;

namespace zFramework.AppBuilder
{
    [Serializable]
    public class FileOperationProfile
    {
        public FileOperationType type;
        public string sourcePath; // 源路径
        public string destinationPath; // 目标路径
        public string newName; // 新名称（仅在重命名时使用）
    }
}