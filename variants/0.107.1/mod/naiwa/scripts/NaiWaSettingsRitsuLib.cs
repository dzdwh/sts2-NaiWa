#if !TOOLS
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace NaiWa.scripts;

/// <summary>
/// 奶蛙设置（大笑动画/音量）迁移到 RitsuLib 设置框架：
/// 注册进游戏 Settings → Mod Settings 页面，由 RitsuLib 持久化（mod_data），
/// 0.107 / 0.111 两个版本共用同一套 API，无需各自维护设置 UI。
/// 控制台命令与卡牌逻辑仍通过 NaiWaSettings 读写这里。
/// </summary>
public static class NaiWaSettingsRitsuLib
{
    private sealed class Model
    {
        public bool EnableLaughAnim { get; set; } = true;
        public double LaughVolume { get; set; } = 0.7;
    }

    private static readonly Model Instance = new();
    private static IModSettingsValueBinding<bool>? _animBinding;
    private static IModSettingsValueBinding<double>? _volumeBinding;

    public static bool EnableLaughAnim
    {
        get => _animBinding?.Read() ?? Instance.EnableLaughAnim;
        set => _animBinding?.Write(value);
    }

    public static double LaughVolume
    {
        get => _volumeBinding?.Read() ?? Instance.LaughVolume;
        set => _volumeBinding?.Write(Math.Clamp(value, 0, 1));
    }

    public static void Register()
    {
        try
        {
            // 0.107.1 的 Profile 绑定在数据键从未写入时 Read 直接抛 KeyNotFoundException，
            // 用 SafeBinding 包装：读失败返回默认值，写正常持久化。
            _animBinding = new SafeBinding<bool>(
                ModSettingsBindings.Profile<Model, bool>("naiwa", "enable_laugh_anim",
                    m => m.EnableLaughAnim, (m, v) => m.EnableLaughAnim = v),
                () => true);
            _volumeBinding = new SafeBinding<double>(
                ModSettingsBindings.Profile<Model, double>("naiwa", "laugh_volume",
                    m => m.LaughVolume, (m, v) => m.LaughVolume = Math.Clamp(v, 0, 1)),
                () => 0.7);

            RitsuLibFramework.RegisterModSettings("naiwa", page => page.AddSection("laugh", section =>
            {
                section.AddToggle("enable_laugh_anim",
                    ModSettingsText.Literal("大笑动画"), _animBinding!);
                section.AddSlider("laugh_volume",
                    ModSettingsText.Literal("大笑音量"), _volumeBinding!, 0, 1, 0.05,
                    v => $"{(int)Math.Round(v * 100)}%");
            }));
            Log.Info("[NaiWaSettings] 已注册到 RitsuLib 设置");
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaSettings] RitsuLib 设置注册失败: {ex.Message}");
        }
    }

    /// <summary>读失败（如 0.107.1 新档数据键缺失）时返回默认值，写操作照常透传。</summary>
    private sealed class SafeBinding<T> : IModSettingsValueBinding<T>
    {
        private readonly IModSettingsValueBinding<T> _inner;
        private readonly Func<T> _default;

        public SafeBinding(IModSettingsValueBinding<T> inner, Func<T> defaultValue)
        {
            _inner = inner;
            _default = defaultValue;
        }

        public T Read()
        {
            try
            {
                return _inner.Read();
            }
            catch
            {
                return _default();
            }
        }

        public void Write(T value) => _inner.Write(value);

        public string ModId => _inner.ModId;

        public string DataKey => _inner.DataKey;

        public SaveScope Scope => _inner.Scope;

        public void Save() => _inner.Save();
    }
}
#endif
