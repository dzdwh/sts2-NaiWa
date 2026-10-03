#if !TOOLS
using System;
using MegaCrit.Sts2.Core.Logging;
using SmartFormat;
using SmartFormat.Core.Extensions;

namespace NaiWa.scripts;

public class NaiWaDirectionFormatter : IFormatter
{
    public string Name
    {
        get => "dir";
        set => throw new NotSupportedException();
    }
    public bool CanAutoDetect { get; set; }

    /// <summary>Called via Harmony after LocManager.LoadLocFormatters completes.</summary>
    public static void OnLocFormattersLoaded()
    {
        Smart.Default.AddExtensions(new NaiWaDirectionFormatter());
        Log.Info("[DirFormatter] Registered via Harmony");
    }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        try
        {
            object val = formattingInfo.CurrentValue;
            int v = 0;
            if (val is IConvertible c)
                v = Convert.ToInt32(c);
            else if (val is decimal d)
                v = (int)d;
            else if (val is int i)
                v = i;
            else
                return false;

            string result = v == 1 ? "上升" : (v == -1 ? "下降" : "?");
            formattingInfo.Write(result);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[DirFormatter] ERROR: {ex}");
            return false;
        }
    }
}
#endif
