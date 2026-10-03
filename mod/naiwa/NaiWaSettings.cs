using NaiWa.scripts;

namespace NaiWa;

/// <summary>
/// Static accessor for laugh settings. Backed by RitsuLib settings framework
/// (NaiWaSettingsRitsuLib), so the UI + persistence live in RitsuLib (mod_data).
/// Console commands: laugh_anim / laugh_vol (registered via NaiWaConsoleCommands).
/// </summary>
public static class NaiWaSettings
{
    public static bool EnableLaughAnim
    {
        get => NaiWaSettingsRitsuLib.EnableLaughAnim;
        set => NaiWaSettingsRitsuLib.EnableLaughAnim = value;
    }

    public static float LaughVolume
    {
        get => (float)NaiWaSettingsRitsuLib.LaughVolume;
        set => NaiWaSettingsRitsuLib.LaughVolume = value;
    }
}
