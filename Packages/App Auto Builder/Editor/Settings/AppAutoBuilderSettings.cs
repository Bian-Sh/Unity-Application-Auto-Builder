using UnityEngine;
using System;

namespace zFramework.AppBuilder
{
    [Serializable]
    public class AppAutoBuilderSettings : ScriptableObject
    {
        public string virboxExePath;
        public string nsisExePath;
        public bool shouldKeepNsisFile;
    }
}
