using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using NaiWa;
using NaiWa.cards;

namespace NaiWa.relics;

/// <summary>
/// 奶王之卵 - 濒死时复活并保留1点血量，选择是否进行奶之刻。
/// 是：删除牌组所有召唤牌（每张+1血量上限+回1血），奖励不再出现召唤牌，随后所有生物大笑。
/// 否：立即死亡。
/// </summary>
public sealed class NaiWaNaiWangZhiLuan : RelicModel
{
    private const string TransitionPreviewPath =
        "res://scenes/vfx/events/nai_zhi_ke_transition_layout.tscn";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override string PackedIconPath =>
        "res://images/relics/nai_wa_nai_wang_zhi_luan.png";

    private bool _wasUsed;

    [SavedProperty]
    public bool WasUsed
    {
        get => _wasUsed;
        set
        {
            AssertMutable();
            _wasUsed = value;
            if (value)
                Status = RelicStatus.Disabled;
        }
    }

    private bool _filterSummonCards;

    [SavedProperty]
    public bool FilterSummonCards
    {
        get => _filterSummonCards;
        set { AssertMutable(); _filterSummonCards = value; }
    }

    /// <summary>濒死：首次阻止死亡（复活）。</summary>
    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner.Creature) return true;
        return WasUsed;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        await CreatureCmd.SetCurrentHp(creature, 1); // 保留1点血量

        try
        {
            Task introBgm = PlayBgm("res://audio/glfs1.mp3"); // 弹出选择框时播放

            // 确认框选择是否进行奶之刻（期间复活保护仍然有效，防多段伤害打死）
            bool doNaiZhiKe = await ShowNaiZhiKeConfirm(introBgm);

            WasUsed = true; // 选完才失效
            if (doNaiZhiKe)
            {
                _ = PlayNaiZhiKeTransition(); // 动画在选"是"之后播放
                await PerformNaiZhiKe();
                await AddNaiZhiShouCopies();
            }
            else
                await CreatureCmd.Kill(base.Owner.Creature); // 否：立即死亡（WasUsed 已 true，死亡生效）
        }
        catch
        {
            WasUsed = true; // 选择异常也失效，避免无限复活保护
            throw;
        }
    }

    /// <summary>奶之刻确认框（保留原版模态、热键和手柄行为，使用专属视觉）。</summary>
    private static async Task<bool> ShowNaiZhiKeConfirm(Task introBgm)
    {
        var popup = NGenericPopup.Create();
        if (popup == null) return false; // TestMode
        StyleNaiZhiKePopup(popup); // 必须在 Add 前设置按钮材质，让 _Ready 记住新的焦点亮度

        var body = new LocString("cards", "NAI_WA_NAI_WANG_ZHI_LUAN_CONFIRM.body");
        var header = new LocString("cards", "NAI_WA_NAI_WANG_ZHI_LUAN_CONFIRM.header");
        var noText = new LocString("cards", "NAI_WA_NAI_WANG_ZHI_LUAN_CONFIRM.no");
        var yesText = new LocString("cards", "NAI_WA_NAI_WANG_ZHI_LUAN_CONFIRM.yes");
        var vertical = popup.GetNode<NVerticalPopup>("VerticalPopup");
        var noButton = popup.GetNode<NPopupYesNoButton>("VerticalPopup/NoButton");
        var yesButton = popup.GetNode<NPopupYesNoButton>("VerticalPopup/YesButton");

        // 音乐期间只显示弹窗内容；暂不初始化按钮，以免确认/取消热键仍能触发。
        vertical.SetText(header, body);
        noButton.Visible = false;
        yesButton.Visible = false;
        popup.Modulate = new Color(1f, 1f, 1f, 0f);
        NModalContainer.Instance.Add(popup);

        var popupFade = popup.CreateTween();
        popupFade.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        popupFade.TweenProperty(popup, "modulate", Colors.White, 0.45f);
        popupFade.Play();

        await introBgm;
        if (!GodotObject.IsInstanceValid(popup)) return false;

        var confirmation = popup.WaitForConfirmation(body, header, noText, yesText);
        noButton.Modulate = new Color(1f, 1f, 1f, 0f);
        yesButton.Modulate = new Color(1f, 1f, 1f, 0f);
        noButton.Visible = true;
        yesButton.Visible = true;

        var buttonFade = popup.CreateTween();
        buttonFade.SetParallel(true);
        buttonFade.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        buttonFade.TweenProperty(noButton, "modulate", Colors.White, 0.3f);
        buttonFade.TweenProperty(yesButton, "modulate", Colors.White, 0.3f);
        buttonFade.Play();

        yesButton.GrabFocus();
        return await confirmation;
    }

    /// <summary>
    /// 奶之刻专属弹窗：使用手绘裂土边框和奶王之卵浮雕，融入原版粗粝 UI 风格。
    /// 仅替换视觉层，NGenericPopup/NPopupYesNoButton 的输入和关闭逻辑保持原样。
    /// </summary>
    private static void StyleNaiZhiKePopup(NGenericPopup popup)
    {
        var vertical = popup.GetNode<NVerticalPopup>("VerticalPopup");
        vertical.SelfModulate = new Color(1f, 1f, 1f, 0f); // 隐藏原版蓝绿弹窗贴图，不影响子节点

        var frame = new Control
        {
            Name = "NaiZhiKeFrame",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
        };
        SetFullRect(frame);
        vertical.AddChild(frame);
        vertical.MoveChild(frame, 0);

        // 手绘贴图中央和外侧都是真透明区域，背景只铺在内框，避免露出方形边角。
        var contentBase = new ColorRect
        {
            Color = new Color(0.018f, 0.01f, 0.008f, 0.98f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        SetNaiZhiKePopupContentRect(contentBase);
        frame.AddChild(contentBase);

        var background = LoadNaiZhiKeBackgroundTexture();
        if (background != null)
        {
            var backgroundRect = new TextureRect
            {
                Texture = background,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Material = MakeNaiZhiKePopupBackgroundMaterial(),
            };
            SetNaiZhiKePopupContentRect(backgroundRect);
            frame.AddChild(backgroundRect);
        }

        var veil = new ColorRect
        {
            Color = new Color(0.012f, 0.006f, 0.004f, 0.56f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        SetNaiZhiKePopupContentRect(veil);
        frame.AddChild(veil);

        var frameTexture = LoadNaiZhiKePopupFrameTexture();
        if (frameTexture != null)
        {
            var shadow = new TextureRect
            {
                Texture = frameTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Modulate = new Color(0.04f, 0.012f, 0.006f, 0.72f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            SetFullRect(shadow);
            shadow.OffsetLeft += 5f;
            shadow.OffsetTop += 8f;
            shadow.OffsetRight += 5f;
            shadow.OffsetBottom += 8f;
            frame.AddChild(shadow);

            var frameArt = new TextureRect
            {
                Name = "NaiZhiKePaintedFrame",
                Texture = frameTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            SetFullRect(frameArt);
            frame.AddChild(frameArt);

            // 奶王之卵使用用户提供的原图独立叠加，不交给生图模型重绘，也不施加材质换色。
            var eggTexture = LoadNaiWangZhiLuanEmblemTexture();
            if (eggTexture != null)
            {
                var egg = new TextureRect
                {
                    Name = "NaiWangZhiLuanOriginalEmblem",
                    Texture = eggTexture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                egg.AnchorLeft = 0.5f;
                egg.AnchorRight = 0.5f;
                egg.OffsetLeft = -52f;
                egg.OffsetTop = 27f;
                egg.OffsetRight = 52f;
                egg.OffsetBottom = 125f;
                frame.AddChild(egg);
            }
        }

        var header = popup.GetNode<Label>("VerticalPopup/Header");
        header.OffsetTop = 168f;
        header.OffsetBottom = 220f;
        header.AddThemeColorOverride("font_color", new Color(0.9f, 0.73f, 0.35f));
        header.AddThemeColorOverride("font_outline_color", new Color(0.16f, 0.08f, 0.025f));
        header.AddThemeConstantOverride("outline_size", 11);

        var body = popup.GetNode<RichTextLabel>("VerticalPopup/Description");
        body.OffsetLeft = 72f;
        body.OffsetTop = 226f;
        body.OffsetRight = -72f;
        body.OffsetBottom = -170f;
        body.AddThemeColorOverride("default_color", new Color(0.92f, 0.86f, 0.74f));
        body.AddThemeColorOverride("font_shadow_color", new Color(0.08f, 0.03f, 0.01f, 0.82f));

        StyleNaiZhiKeButton(popup, "NoButton", new Color(0.27f, 0.16f, 0.13f),
            new Color(0.82f, 0.73f, 0.62f));
        StyleNaiZhiKeButton(popup, "YesButton", new Color(0.62f, 0.2f, 0.055f),
            new Color(0.95f, 0.79f, 0.44f));
    }

    private static void SetFullRect(Control control, float margin = 0f)
    {
        control.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        control.OffsetLeft = margin;
        control.OffsetTop = margin;
        control.OffsetRight = -margin;
        control.OffsetBottom = -margin;
    }

    private static void SetNaiZhiKePopupContentRect(Control control)
    {
        control.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        control.OffsetLeft = 66f;
        control.OffsetTop = 146f;
        control.OffsetRight = -66f;
        control.OffsetBottom = -58f;
    }

    private static void StyleNaiZhiKeButton(
        NGenericPopup popup, string buttonName, Color tint, Color textColor)
    {
        var image = popup.GetNode<TextureRect>($"VerticalPopup/{buttonName}/Visuals/Image");
        image.Material = MakeNaiZhiKeButtonMaterial(tint);

        var label = popup.GetNode<Label>($"VerticalPopup/{buttonName}/Visuals/Label");
        label.AddThemeColorOverride("font_color", textColor);
        label.AddThemeColorOverride("font_outline_color", new Color(0.18f, 0.008f, 0.004f));
    }

    private static ShaderMaterial MakeNaiZhiKePopupBackgroundMaterial()
    {
        var shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                void fragment() {
                    vec4 src = texture(TEXTURE, UV);
                    float luma = dot(src.rgb, vec3(0.299, 0.587, 0.114));
                    vec3 ember = mix(vec3(luma * 0.12), src.rgb * vec3(0.72, 0.28, 0.2), 0.72);
                    float vignette = 1.0 - smoothstep(0.28, 0.78, length(UV - vec2(0.5)));
                    ember *= 0.48 + vignette * 0.52;
                    COLOR = vec4(ember, src.a * 0.55);
                }
                """,
        };
        return new ShaderMaterial { Shader = shader };
    }

    private static ShaderMaterial MakeNaiZhiKeButtonMaterial(Color tint)
    {
        var shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                uniform vec4 tint : source_color = vec4(1.0);
                uniform float s = 0.9;
                uniform float v = 0.78;
                void fragment() {
                    vec4 src = texture(TEXTURE, UV);
                    float luma = dot(src.rgb, vec3(0.299, 0.587, 0.114));
                    vec3 colored = tint.rgb * (0.34 + luma * 0.9);
                    float coloredLuma = dot(colored, vec3(0.299, 0.587, 0.114));
                    colored = mix(vec3(coloredLuma), colored, s);
                    COLOR = vec4(colored * v, src.a * tint.a);
                }
                """,
        };
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("tint", tint);
        material.SetShaderParameter("s", 0.9f);
        material.SetShaderParameter("v", 0.78f);
        return material;
    }

    private static Texture2D? LoadNaiZhiKePopupFrameTexture()
    {
        var texture = ResourceLoader.Load<Texture2D>("res://images/ui/nai_zhi_ke_popup_frame.png");
        if (texture != null) return texture;

        // 开发期回退：Godot 尚未导入新资源时，允许本机直接预览生成的手绘边框。
        var image = Image.LoadFromFile(
            @"E:\Engineering\sljt\mod\naiwa\images\ui\nai_zhi_ke_popup_frame.png");
        return image == null ? null : ImageTexture.CreateFromImage(image);
    }

    private static Texture2D? LoadNaiWangZhiLuanEmblemTexture()
    {
        var texture = ResourceLoader.Load<Texture2D>(
            "res://images/ui/nai_wang_zhi_luan_emblem.png");
        if (texture != null) return texture;

        // 开发期回退同样读取用户原图，保证本机预览与 PCK 内资源完全一致。
        var image = Image.LoadFromFile(@"E:\Engineering\sljt\sucai\遗物\bnzn.png");
        return image == null ? null : ImageTexture.CreateFromImage(image);
    }

    private static Texture2D? LoadNaiZhiKeBackgroundTexture()
    {
        var texture = ResourceLoader.Load<Texture2D>("res://images/rooms/naizhike.png");
        if (texture != null) return texture;

        // 开发期回退：PCK 未导出时直接加载源图（发布走 res://）
        var image = Image.LoadFromFile(@"E:\Engineering\sljt\sucai\背景图\naizhike.png");
        return image == null ? null : ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// 读取 Godot 预览场景中的移动遗物位置与尺寸。
    /// EggTarget 的中心同时作为飞入终点和背景扩散圆心；X 以画布中心为基准，
    /// 因此在宽屏分辨率下仍会保持相同的相对偏移。
    /// </summary>
    private static (Vector2 centerOffset, Vector2 iconSize) LoadTransitionPreviewLayout()
    {
        var fallback = (new Vector2(0f, 238f), new Vector2(64f, 64f));
        var packed = ResourceLoader.Load<PackedScene>(TransitionPreviewPath);
        if (packed == null) return fallback;

        Control? preview = null;
        try
        {
            preview = packed.Instantiate<Control>();
            var eggTarget = preview.GetNodeOrNull<Control>("EggTarget");
            if (eggTarget == null) return fallback;

            var previewSize = new Vector2(
                preview.Size.X > 0f ? preview.Size.X : preview.CustomMinimumSize.X,
                preview.Size.Y > 0f ? preview.Size.Y : preview.CustomMinimumSize.Y);
            if (previewSize.X <= 0f) previewSize.X = 1920f;
            if (previewSize.Y <= 0f) previewSize.Y = 1080f;

            // 场景尚未进入 SceneTree 时，Control.Position 不会可靠地解析锚点。
            // 直接按 anchors + offsets 还原 Inspector 中看到的实际矩形。
            var targetCenter = new Vector2(
                previewSize.X * (eggTarget.AnchorLeft + eggTarget.AnchorRight) / 2f
                    + (eggTarget.OffsetLeft + eggTarget.OffsetRight) / 2f,
                previewSize.Y * (eggTarget.AnchorTop + eggTarget.AnchorBottom) / 2f
                    + (eggTarget.OffsetTop + eggTarget.OffsetBottom) / 2f);
            var iconSize = new Vector2(
                previewSize.X * (eggTarget.AnchorRight - eggTarget.AnchorLeft)
                    + eggTarget.OffsetRight - eggTarget.OffsetLeft,
                previewSize.Y * (eggTarget.AnchorBottom - eggTarget.AnchorTop)
                    + eggTarget.OffsetBottom - eggTarget.OffsetTop);
            if (iconSize.X <= 0f || iconSize.Y <= 0f)
                iconSize = fallback.Item2;

            return (new Vector2(targetCenter.X - previewSize.X / 2f, targetCenter.Y), iconSize);
        }
        finally
        {
            if (preview != null && GodotObject.IsInstanceValid(preview))
                preview.Free();
        }
    }

    /// <summary>
    /// 奶之刻动态转场（纯视觉）：
    /// 1) 奶王之卵遗物从状态栏飞到屏幕中央并放大
    /// 2) 逐渐变灰
    /// 3) 新背景从遗物位置呈圈状扩散铺满全屏
    /// 4) 灰色遗物飞回状态栏（落地后恢复彩色）
    /// </summary>
    private static async Task PlayNaiZhiKeTransition()
    {
        try
        {
            var room = NCombatRoom.Instance;
            var bg = room?.GetNodeOrNull<Control>("%BgContainer");
            var top = NRun.Instance?.GlobalUi?.AboveTopBarVfxContainer;
            if (bg == null || top == null) return;

            var holder = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes
                .FirstOrDefault(h => h.Relic?.Model is NaiWaNaiWangZhiLuan);
            if (holder == null || holder.Relic?.Icon == null) return;

            // 用临时克隆图标做动画，原状态栏图标保持不动（期间隐藏，结束恢复）——
            // 直接 reparent 原 Icon 会继承 FullRect anchors 铺满全屏（实测 bug）。
            var realIcon = holder.Relic.Icon;
            var iconTexture = realIcon.Texture;
            if (iconTexture == null) return;
            var startCenter = realIcon.GlobalPosition + realIcon.Size / 2f;
            var (centerOffset, iconSize) = LoadTransitionPreviewLayout();
            var center = new Vector2(
                top.Size.X / 2f + centerOffset.X,
                centerOffset.Y);

            var fake = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Texture = iconTexture,
                Size = iconSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                PivotOffset = iconSize / 2f,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            fake.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            top.AddChild(fake);
            var startPosition = startCenter - top.GlobalPosition - fake.Size / 2f;
            fake.Position = startPosition;
            realIcon.Visible = false; // 动画期间隐藏原图标，视觉上是同一个遗物飞出去

            // 1) 飞到屏幕顶部中央
            var fly = top.CreateTween();
            fly.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
            fly.TweenProperty(fake, "position", center - fake.Size / 2f, 0.4f);
            fly.Parallel().TweenProperty(fake, "scale", Vector2.One, 0.4f);
            fly.Play();
            await Task.Delay(450);
            if (!GodotObject.IsInstanceValid(fake)) return;

            // 2) 逐渐变灰
            var grayMat = MakeGrayMaterial();
            fake.Material = grayMat;
            var grayTween = top.CreateTween();
            grayTween.TweenProperty(grayMat, "shader_parameter/gray", 1f, 0.3f);
            grayTween.Play();
            await Task.Delay(330);
            if (!GodotObject.IsInstanceValid(fake)) return;

            // 3) 圈状扩散新背景（中心 = 遗物当前位置）
            var tex = LoadNaiZhiKeBackgroundTexture();
            if (tex == null) return;

            // BgContainer 是 0×0 锚点容器（原背景层用大 offset 定位），FullRect 不生效，
            // 改挂全屏的 CombatSceneContainer 并插到 BgContainer 之后（背景之上、角色之下）。
            var scene = room.GetNodeOrNull<Control>("%CombatSceneContainer");
            if (scene == null) return;
            if (scene.GetNodeOrNull("NaiWaNaiWangZhiLuanBg") != null) return; // 已触发过
            var bgNode = room.GetNodeOrNull<Control>("%BgContainer");
            int insertIdx = bgNode != null ? bgNode.GetIndex() + 1 : 0;

            var rect = new TextureRect
            {
                Name = "NaiWaNaiWangZhiLuanBg",
                Texture = tex,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            scene.AddChild(rect);
            scene.MoveChild(rect, insertIdx);

            var reveal = MakeRevealMaterial(rect.Size);
            rect.Material = reveal;
            var revealCenter = new Vector2(
                rect.Size.X / 2f + centerOffset.X,
                centerOffset.Y);
            reveal.Set("shader_parameter/center", revealCenter);
            var farthestCorner = new Vector2(
                Mathf.Max(revealCenter.X, rect.Size.X - revealCenter.X),
                Mathf.Max(revealCenter.Y, rect.Size.Y - revealCenter.Y));
            float maxRadius = farthestCorner.Length();

            var revealTween = scene.CreateTween();
            revealTween.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            revealTween.TweenProperty(reveal, "shader_parameter/radius", maxRadius, 2.2f);
            revealTween.Play();
            await Task.Delay(2250);

            // 直接复用原版“荣耀”背景的星点、灰烬和薄雾粒子，仅换成奶之刻配色。
            AddRecoloredOriginalBackgroundVfx(scene, insertIdx + 1, rect.Size);

            // 4) 灰色遗物飞回状态栏（保持灰色，落地后恢复彩色）
            var back = top.CreateTween();
            back.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
            back.TweenProperty(fake, "position", startPosition, 0.4f);
            back.Parallel().TweenProperty(fake, "scale", Vector2.One, 0.4f);
            back.Play();
            await Task.Delay(450);

            // 5) 恢复原图标，销毁临时图标
            if (GodotObject.IsInstanceValid(realIcon))
                realIcon.Visible = true;
            if (GodotObject.IsInstanceValid(fake))
            {
                fake.QueueFree();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[NaiWaNaiWangZhiLuan] transition failed: {ex.Message}");
        }
    }

    private static ShaderMaterial MakeGrayMaterial()
    {
        var shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                uniform float gray : hint_range(0.0, 1.0) = 0.0;
                void fragment() {
                    vec4 c = texture(TEXTURE, UV);
                    float g = dot(c.rgb, vec3(0.299, 0.587, 0.114));
                    c.rgb = mix(c.rgb, vec3(g), gray);
                    COLOR = c;
                }
                """,
        };
        return new ShaderMaterial { Shader = shader };
    }

    private static ShaderMaterial MakeRevealMaterial(Vector2 size)
    {
        var shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                uniform vec2 resolution;
                uniform vec2 center = vec2(1024.0, 480.0);
                uniform float radius : hint_range(0.0, 1600.0) = 0.0;
                void fragment() {
                    vec2 p = UV * resolution;
                    if (distance(p, center) > radius) discard;
                    COLOR = texture(TEXTURE, UV);
                }
                """,
        };
        var mat = new ShaderMaterial { Shader = shader };
        mat.Set("shader_parameter/resolution", size);
        mat.Set("shader_parameter/radius", 0f);
        return mat;
    }

    private static void AddRecoloredOriginalBackgroundVfx(
        Control scene, int insertIdx, Vector2 size)
    {
        var packed = ResourceLoader.Load<PackedScene>(
            "res://scenes/backgrounds/glory/glory_background.tscn");
        if (packed == null) return;

        var source = packed.Instantiate();
        try
        {
            var layer = new Control
            {
                Name = "NaiWaNaiWangZhiLuanVfx",
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            layer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            scene.AddChild(layer);
            scene.MoveChild(layer, insertIdx);

            MoveOriginalParticle(source, layer, "stars",
                new Vector2(size.X / 2f, size.Y * 0.24f),
                new Color(1f, 0.18f, 0.045f, 0.72f));
            MoveOriginalParticle(source, layer, "Foreground/ash",
                new Vector2(size.X / 2f, -20f),
                new Color(0.5f, 0.3f, 0.24f, 0.62f));
            MoveOriginalParticle(source, layer, "Foreground/ash2",
                new Vector2(size.X / 2f, -20f),
                new Color(1f, 0.2f, 0.04f, 0.72f));

            var fog = MoveOriginalParticle(source, layer, "Foreground/fog4",
                new Vector2(-size.X * 0.65f, size.Y * 0.72f), Colors.White);
            if (fog != null)
                fog.Color = new Color(0.22f, 0.012f, 0.006f, 0.24f);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(source))
                source.Free();
        }
    }

    private static CpuParticles2D? MoveOriginalParticle(
        Node source, Control layer, string path, Vector2 position, Color tint)
    {
        var particle = source.GetNodeOrNull<CpuParticles2D>(path);
        if (particle == null) return null;

        particle.Owner = null;
        particle.GetParent()?.RemoveChild(particle);
        layer.AddChild(particle);
        particle.Position = position;
        particle.SelfModulate = tint;
        particle.Preprocess = Mathf.Min(particle.Lifetime, 10f);
        particle.Emitting = true;
        particle.Restart();
        return particle;
    }

    private async Task PerformNaiZhiKe()
    {
        _ = PlayBgm("res://audio/glfs2.mp3"); // 选择"是"时播放

        // 额外恢复 20% 最大生命值
        if (base.Owner.Creature.IsAlive)
            await CreatureCmd.Heal(base.Owner.Creature, base.Owner.Creature.MaxHp * 0.2m);

        // 删除牌组中所有带召唤效果的牌（计数：每张+1血量上限并回复1点血量）
        var summonCards = base.Owner.Deck.Cards
            .Where(c => NaiWaKeywords.SummonCardTypes.Contains(c.GetType()))
            .ToList();
        if (summonCards.Count > 0)
            await CardPileCmd.RemoveFromDeck(summonCards);

        foreach (var _ in summonCards)
        {
            await CreatureCmd.GainMaxHp(base.Owner.Creature, 1);
            await CreatureCmd.Heal(base.Owner.Creature, 1);
        }

        // 删除牌及其生命结算完成后，全体同时大笑；之后才开始消耗战斗牌。
        await NaiWaZhongBin.LaughAllCreaturesSimultaneously(
            new ThrowingPlayerChoiceContext(), base.Owner.Creature);

        // 手牌/弃牌堆/抽牌堆中的召唤牌也消耗（不计数）
        foreach (var pile in new[] { PileType.Hand, PileType.Discard, PileType.Draw })
        {
            var combatCards = pile.GetPile(base.Owner).Cards
                .Where(c => NaiWaKeywords.SummonCardTypes.Contains(c.GetType()))
                .ToList();
            foreach (var card in combatCards)
                await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), card);
        }

        // 战斗中触发：杀死所有随从献祭
        if (CombatManager.Instance.IsInProgress)
        {
            var pets = base.Owner.Creature.CombatState.Allies
                .Where(a => a.IsAlive && a.PetOwner == base.Owner)
                .ToList();
            foreach (var pet in pets)
                await CreatureCmd.Kill(pet);
        }

        // 卡牌奖励中不再出现带召唤效果的牌
        FilterSummonCards = true;
    }

    /// <summary>永久向牌组添加一张奶之手，并在当前战斗手牌中添加一张临时副本。</summary>
    private async Task AddNaiZhiShouCopies()
    {
        var deckCard = base.Owner.RunState.CreateCard<NaiWaNaiZhiShou>(base.Owner);
        await CardPileCmd.Add(deckCard, PileType.Deck);

        if (!CombatManager.Instance.IsInProgress || base.Owner.Creature.CombatState == null)
            return;

        var handCard = base.Owner.Creature.CombatState.CreateCard<NaiWaNaiZhiShou>(base.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(handCard, PileType.Hand, base.Owner);
    }

    /// <summary>播放自定义音频（照 NaiWaXiao.TriggerLaugh 的播放模式）。</summary>
    private Task PlayBgm(string path)
    {
        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null) return Task.CompletedTask;

        var finished = new TaskCompletionSource();
        var audioPlayer = new AudioStreamPlayer();
        audioPlayer.Stream = stream;
        audioPlayer.Finished += () =>
        {
            finished.TrySetResult();
            if (GodotObject.IsInstanceValid(audioPlayer))
                audioPlayer.QueueFree();
        };
        audioPlayer.TreeExiting += () => finished.TrySetResult();

        var room = NCombatRoom.Instance;
        if (room != null)
        {
            var cn = room.GetCreatureNode(base.Owner.Creature);
            var parent = cn?.Visuals?.GetNodeOrNull<Node2D>("%Visuals") ?? (Godot.Node)room;
            parent.AddChild(audioPlayer);
        }
        else
        {
            ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(audioPlayer);
        }

        audioPlayer.Play();
        return finished.Task;
    }

    /// <summary>奖励过滤：已选"是"后，卡牌奖励不再出现召唤牌。</summary>
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner || !FilterSummonCards) return false;
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is not CardReward cr) continue;
            var filtered = cr.Cards.Where(c => !NaiWaKeywords.SummonCardTypes.Contains(c.GetType())).ToList();
            if (filtered.Count == cr.Cards.Count()) continue;
            var replacement = new CardReward(filtered, CardCreationSource.Encounter, player,
                CardCreationOptions.ForRoom(player, RoomType.Monster));
            rewards[i] = replacement;
            return true;
        }
        return false;
    }
}
