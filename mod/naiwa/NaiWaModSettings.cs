namespace NaiWa;

/// <summary>
/// Simple bridge: delegates to NaiWaSettings (stored in naiwa_save.save).
/// Console commands: laugh_anim / laugh_vol
/// Does NOT require RitsuLib.
/// </summary>
public static class NaiWaModSettings
{
    public static bool EnableLaughAnim
    {
        get => NaiWaSettings.EnableLaughAnim;
        set => NaiWaSettings.EnableLaughAnim = value;
    }

    public static double LaughVolume
    {
        get => NaiWaSettings.LaughVolume;
        set => NaiWaSettings.LaughVolume = (float)value;
    }
}
