using SengokuScroll.Common.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SengokuScroll.Common.Extensions;

public static  class CommonExtensions
{
    public static string ToJson(this object o) => JsonUtils.ToJson(o);
}
