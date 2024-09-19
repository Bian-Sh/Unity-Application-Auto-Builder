using System;

namespace zFramework.AppBuilder
{
    [Flags]
    public enum BuildOptionsLit
    {
        Development = 0x01,
        AutoRunPlayer = 0x02,
        ShowBuiltPlayer = 0x04,
    }
}
