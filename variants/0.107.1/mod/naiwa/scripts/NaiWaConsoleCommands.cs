using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace NaiWa.scripts;

/// <summary>Toggle laugh animation on/off. Usage: laugh_anim</summary>
public class LaughAnimConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "laugh_anim";
    public override string Args => "";
    public override string Description => "开关大笑动画";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        NaiWaSettings.EnableLaughAnim = !NaiWaSettings.EnableLaughAnim;
        return new CmdResult(true, $"大笑动画: {(NaiWaSettings.EnableLaughAnim ? "开启" : "关闭")}");
    }
}

/// <summary>Set laugh volume. Usage: laugh_vol 0.5</summary>
public class LaughVolConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "laugh_vol";
    public override string Args => "<0.0-1.0>";
    public override string Description => "设置大笑音量 (0.0=静音, 1.0=最大)";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length > 0 && float.TryParse(args[0], out float vol))
        {
            NaiWaSettings.LaughVolume = vol;
            return new CmdResult(true, $"大笑音量已设为 {vol:F1}");
        }
        return new CmdResult(true, $"当前大笑音量: {NaiWaSettings.LaughVolume:F1}  (用法: laugh_vol 0.5)");
    }
}
