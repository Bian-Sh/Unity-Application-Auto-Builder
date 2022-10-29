using System;

namespace zFramework.Extension
{
    [Flags]
    public enum BuildOptionsLit
    {
        Development = 0x01,
        AutoRunPlayer = 0x02,
        ShowBuiltPlayer = 0x04,
    }
}
