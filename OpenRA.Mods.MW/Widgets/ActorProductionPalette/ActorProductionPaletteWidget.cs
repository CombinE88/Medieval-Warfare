using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.MW.Traits.BuildingTraits;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Widgets.ActorProductionPalette
{
    public class UpgradeProductionIcon
    {
        public ActorInfo Actor;
        public string Name;
        public HotkeyReference Hotkey;
        public Sprite Sprite;
        public PaletteReference Palette;
        public PaletteReference IconClockPalette;
        public PaletteReference IconDarkenPalette;
        public float2 Pos;
        public List<UpgradeProductionItem> Queued;
        public UpgradeProductionQueue UpgradeProductionQueue;
    }

    public class ActorProductionPaletteWidget : Widget
    {
        public enum ReadyTextStyleOptions
        {
            Solid,
            AlternatingColor,
            Blinking
        }

        public readonly ReadyTextStyleOptions ReadyTextStyle = ReadyTextStyleOptions.AlternatingColor;
        public readonly Color ReadyTextAltColor = Color.Gold;
        public readonly int2 IconSize = new int2(64, 48);
        public readonly int2 IconMargin = int2.Zero;
        public readonly int2 IconSpriteOffset = int2.Zero;

        public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
        public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");
        public readonly string TooltipContainer;
        public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";

        // Note: LinterHotkeyNames assumes that these are disabled by default
        public readonly string HotkeyPrefix = null;
        public readonly int HotkeyCount = 0;

        public readonly string ClockAnimation = "clock";
        public readonly string ClockSequence = "idle";
        public readonly string ClockPalette = "chrome";

        public readonly string NotBuildableAnimation = "clock";
        public readonly string NotBuildableSequence = "idle";
        public readonly string NotBuildablePalette = "chrome";

        public readonly bool DrawTime = true;

        [Translate] public readonly string ReadyText = "";
        [Translate] public readonly string HoldText = "";
        [Translate] public readonly string InfiniteSymbol = "\u221E";

        public int DisplayedIconCount { get; private set; }
        public int TotalIconCount { get; private set; }
        public event Action<int, int> OnIconCountChanged = (a, b) => { };

        public ProductionIcon TooltipIcon { get; private set; }
        public Func<ProductionIcon> GetTooltipIcon;
        public readonly World World;
        readonly ModData modData;
        readonly OrderManager orderManager;

        public int MinimumRows = 1;
        public int MaximumRows = int.MaxValue;

        public int IconRowOffset = 0;
        public int MaxIconRowOffset = int.MaxValue;

        Lazy<TooltipContainerWidget> tooltipContainer;
        UpgradeProductionQueue currentQueue;
        HotkeyReference[] hotkeys;

        public UpgradeProductionQueue CurrentQueue
        {
            get
            {
                return currentQueue;
            }

            set
            {
                currentQueue = value;
                RefreshIcons();
            }
        }

        public override Rectangle EventBounds
        {
            get { return eventBounds; }
        }

        Dictionary<Rectangle, UpgradeProductionIcon> icons = new Dictionary<Rectangle, UpgradeProductionIcon>();
        Animation cantBuild, clock;
        Rectangle eventBounds = Rectangle.Empty;

        readonly WorldRenderer worldRenderer;

        SpriteFont overlayFont, symbolFont;
        float2 holdOffset, readyOffset, timeOffset, queuedOffset, infiniteOffset;

        private readonly Actor factoryActor;
        private readonly ActorProductionPaletteManagerWidget manager;

        [CustomLintableHotkeyNames]
        public static IEnumerable<string> LinterHotkeyNames(MiniYamlNode widgetNode, Action<string> emitError, Action<string> emitWarning)
        {
            var prefix = "";
            var prefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "HotkeyPrefix");
            if (prefixNode != null)
                prefix = prefixNode.Value.Value;

            var count = 0;
            var countNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "HotkeyCount");
            if (countNode != null)
                count = FieldLoader.GetValue<int>("HotkeyCount", countNode.Value.Value);

            if (count == 0)
                return new string[0];

            if (string.IsNullOrEmpty(prefix))
                emitError("{0} must define HotkeyPrefix if HotkeyCount > 0.".F(widgetNode.Location));

            return Exts.MakeArray(count, i => prefix + (i + 1).ToString("D2"));
        }

        public ActorProductionPaletteWidget(ModData modData,
            OrderManager orderManager,
            ActorProductionPaletteManagerWidget manager,
            Actor factoryActor,
            WorldRenderer worldRenderer,
            World world,
            UpgradeProductionQueue productionQueue)
        {
            this.factoryActor = factoryActor;
            this.worldRenderer = worldRenderer;
            World = world;
            CurrentQueue = productionQueue;
            this.manager = manager;

            TooltipContainer = "TOOLTIP_CONTAINER";
            ReadyText = "Ready";
            HoldText = "Paused";
            IconSize = new int2(100, 80);
            MinimumRows = 1;
            MaximumRows = 10;
            IconMargin = new int2(2, 2);

            Bounds.Width = 120;
            Bounds.Height = 20 + productionQueue.AllItems().Count() * 80;

            icons = new Dictionary<Rectangle, UpgradeProductionIcon>();

            this.modData = modData;
            this.orderManager = orderManager;
            GetTooltipIcon = () => TooltipIcon;
            tooltipContainer = Exts.Lazy(() =>
                Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

            cantBuild = new Animation(world, NotBuildableAnimation);
            cantBuild.PlayFetchIndex(NotBuildableSequence, () => 0);
            clock = new Animation(world, ClockAnimation);

            overlayFont = Game.Renderer.Fonts["TinyBold"];
            Game.Renderer.Fonts.TryGetValue("Symbols", out symbolFont);

            UpdatePosition();
        }

        public IEnumerable<ActorInfo> AllBuildables
        {
            get
            {
                if (CurrentQueue == null)
                    return Enumerable.Empty<ActorInfo>();

                return CurrentQueue.AllItems().OrderBy(a => a.TraitInfo<BuildableInfo>().BuildPaletteOrder);
            }
        }

        public override void Initialize(WidgetArgs args)
        {
            base.Initialize(args);

            hotkeys = Exts.MakeArray(HotkeyCount,
                i => modData.Hotkeys[HotkeyPrefix + (i + 1).ToString("D2")]);
        }

        public override void Tick()
        {
            TotalIconCount = AllBuildables.Count();

            if (CurrentQueue != null && !CurrentQueue.Actor.IsInWorld)
                CurrentQueue = null;

            if (CurrentQueue != null)
                RefreshIcons();
        }

        // TODO: Fix tooltips
        public override void MouseEntered()
        {
            if (TooltipContainer != null)
                tooltipContainer.Value.SetTooltip(TooltipTemplate,
                    new WidgetArgs() { { "player", World.LocalPlayer }, { "getTooltipIcon", GetTooltipIcon } });
        }

        public override void MouseExited()
        {
            if (TooltipContainer != null)
                tooltipContainer.Value.RemoveTooltip();
        }

        public override bool HandleMouseInput(MouseInput mi)
        {
            var icon = icons.Where(i => i.Key.Contains(mi.Location))
                .Select(i => i.Value).FirstOrDefault();

            if (mi.Event == MouseInputEvent.Move && icon != null)
                TooltipIcon = new ProductionIcon
                {
                    Sprite = icon.Sprite,
                    Actor = icon.Actor,
                    Name = icon.Name,
                    Hotkey = icon.Hotkey,
                    Palette = icon.Palette,
                    IconClockPalette = icon.IconClockPalette,
                    IconDarkenPalette = icon.IconDarkenPalette,
                    Pos = icon.Pos
                };

            if (icon == null)
                return false;

            // Eat mouse-up events
            if (mi.Event != MouseInputEvent.Down)
                return true;

            return HandleEvent(icon, mi.Button, mi.Modifiers);
        }

        bool HandleEvent(UpgradeProductionIcon icon, MouseButton btn, Modifiers modifiers)
        {
            var startCount = modifiers.HasModifier(Modifiers.Shift) ? 5 : 1;

            // PERF: avoid an unnecessary enumeration by casting back to its known type
            var cancelCount = modifiers.HasModifier(Modifiers.Ctrl) ? ((List<UpgradeProductionItem>)CurrentQueue.AllQueued()).Count : startCount;
            var item = icon.Queued.FirstOrDefault();
            var handled = btn == MouseButton.Left ? HandleLeftClick(item, icon, startCount, modifiers)
                : btn == MouseButton.Right ? HandleRightClick(item, icon, cancelCount)
                : btn == MouseButton.Middle ? HandleMiddleClick(item, icon, cancelCount)
                : false;

            if (!handled)
                Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickDisabledSound, null);

            return true;
        }

        bool HandleLeftClick(UpgradeProductionItem item, UpgradeProductionIcon icon, int handleCount, Modifiers modifiers)
        {
            if (item != null && item.Paused)
            {
                // Resume a paused item
                Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
                Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.QueuedAudio,
                    World.LocalPlayer.Faction.InternalName);
                World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, false));
                return true;
            }

            var buildable = CurrentQueue.BuildableItems().FirstOrDefault(a => a.Name == icon.Name);

            if (buildable != null)
            {
                // Queue a new item
                Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
                string notification;
                var canQueue = CurrentQueue.CanQueue(buildable, out notification);

                if (!CurrentQueue.AllQueued().Any())
                    Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", notification, World.LocalPlayer.Faction.InternalName);

                if (canQueue)
                {
                    var queued = !modifiers.HasModifier(Modifiers.Ctrl);
                    World.IssueOrder(Order.StartProduction(CurrentQueue.Actor, icon.Name, handleCount, queued));
                    return true;
                }
            }

            return false;
        }

        bool HandleRightClick(UpgradeProductionItem item, UpgradeProductionIcon icon, int handleCount)
        {
            if (item == null)
                return false;

            Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);

            if (CurrentQueue.Info.DisallowPaused || item.Paused || item.Done || item.TotalCost == item.RemainingCost)
            {
                // Instantly cancel items that haven't started, have finished, or if the queue doesn't support pausing
                Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio,
                    World.LocalPlayer.Faction.InternalName);
                World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name, handleCount));
            }
            else
            {
                // Pause an existing item
                Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.OnHoldAudio,
                    World.LocalPlayer.Faction.InternalName);
                World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, true));
            }

            return true;
        }

        bool HandleMiddleClick(UpgradeProductionItem item, UpgradeProductionIcon icon, int handleCount)
        {
            if (item == null)
                return false;

            // Directly cancel, skipping "on-hold"
            Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
            Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio,
                World.LocalPlayer.Faction.InternalName);
            World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name, handleCount));

            return true;
        }

        public void RefreshIcons()
        {
            icons = new Dictionary<Rectangle, UpgradeProductionIcon>();
            var producer = CurrentQueue != null ? CurrentQueue.MostLikelyProducer() : default(TraitPair<Production>);
            if (CurrentQueue == null || producer.Trait == null)
            {
                if (DisplayedIconCount != 0)
                {
                    OnIconCountChanged(DisplayedIconCount, 0);
                    DisplayedIconCount = 0;
                }

                return;
            }

            var oldIconCount = DisplayedIconCount;
            DisplayedIconCount = 0;

            var rb = RenderBounds;
            var faction = producer.Trait.Faction;

            foreach (var item in AllBuildables.Skip(IconRowOffset).Take(MaxIconRowOffset))
            {
                var x = 0;
                var y = DisplayedIconCount;
                var rect = new Rectangle(rb.X + x * (IconSize.X + IconMargin.X) + 10, rb.Y + y * (IconSize.Y + IconMargin.Y) + 10, IconSize.X, IconSize.Y);

                var rsi = item.TraitInfo<RenderSpritesInfo>();
                var icon = new Animation(World, rsi.GetImage(item, World.Map.Rules.Sequences, faction));
                var bi = item.TraitInfo<UpgradeBuildableInfo>();
                icon.Play(bi.Icon);

                var pi = new UpgradeProductionIcon()
                {
                    Actor = item,
                    Name = item.Name,
                    Hotkey = DisplayedIconCount < HotkeyCount ? hotkeys[DisplayedIconCount] : null,
                    Sprite = icon.Image,
                    Palette = worldRenderer.Palette(bi.IconPalette),
                    IconClockPalette = worldRenderer.Palette(ClockPalette),
                    IconDarkenPalette = worldRenderer.Palette(NotBuildablePalette),
                    Pos = new float2(rect.Location),
                    Queued = currentQueue.AllQueued().Where(a => a.Item == item.Name).ToList(),
                    UpgradeProductionQueue = currentQueue
                };

                icons.Add(rect, pi);
                DisplayedIconCount++;
            }

            eventBounds = icons.Any() ? icons.Keys.Aggregate(Rectangle.Union) : Rectangle.Empty;

            if (oldIconCount != DisplayedIconCount)
                OnIconCountChanged(oldIconCount, DisplayedIconCount);
        }

        public override void Draw()
        {
            UpdatePosition();
            WidgetUtils.DrawPanel(manager.Background, RenderBounds);
            var iconOffset = 0.5f * IconSize.ToFloat2() + IconSpriteOffset;

            timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0, World.Timestep)) / 2;
            queuedOffset = new float2(4, 2);
            holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
            readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;

            if (ChromeMetrics.TryGet("InfiniteOffset", out infiniteOffset))
                infiniteOffset += queuedOffset;
            else
                infiniteOffset = queuedOffset;

            if (CurrentQueue == null)
                return;

            var buildableItems = CurrentQueue.BuildableItems();

            var pios = currentQueue.Actor.Owner.PlayerActor.TraitsImplementing<IProductionIconOverlay>();

            DisplayedIconCount = 0;
            var rb = RenderBounds;

            // Icons
            foreach (var icon in icons.Values)
            {
                var x = 0;
                var y = DisplayedIconCount;
                var rect = new Rectangle(rb.X + x * (IconSize.X + IconMargin.X) + 10, rb.Y + y * (IconSize.Y + IconMargin.Y) + 10, IconSize.X, IconSize.Y);
                icon.Pos = new float2(rect.Location);

                DisplayedIconCount++;

                WidgetUtils.DrawSHPCentered(icon.Sprite, icon.Pos + iconOffset, icon.Palette);

                // Draw the ProductionIconOverlay's sprite
                var pio = pios.FirstOrDefault(p => p.IsOverlayActive(icon.Actor));
                if (pio != null)
                    WidgetUtils.DrawSHPCentered(pio.Sprite, icon.Pos + iconOffset + pio.Offset(IconSize), worldRenderer.Palette(pio.Palette), 1f);

                // Build progress
                if (icon.Queued.Count > 0)
                {
                    var first = icon.Queued[0];
                    clock.PlayFetchIndex(ClockSequence,
                        () => (first.TotalTime - first.RemainingTime)
                              * (clock.CurrentSequence.Length - 1) / first.TotalTime);
                    clock.Tick();

                    WidgetUtils.DrawSHPCentered(clock.Image, icon.Pos + iconOffset, icon.IconClockPalette);
                }
                else if (!buildableItems.Any(a => a.Name == icon.Name))
                    WidgetUtils.DrawSHPCentered(cantBuild.Image, icon.Pos + iconOffset, icon.IconDarkenPalette);
            }

            // Overlays
            foreach (var icon in icons.Values)
            {
                var total = icon.Queued.Count;
                if (total > 0)
                {
                    var first = icon.Queued[0];
                    var waiting = !CurrentQueue.IsProducing(first) && !first.Done;
                    if (first.Done)
                    {
                        if (ReadyTextStyle == ReadyTextStyleOptions.Solid || orderManager.LocalFrameNumber * worldRenderer.World.Timestep / 360 % 2 == 0)
                            overlayFont.DrawTextWithContrast(ReadyText, icon.Pos + readyOffset, Color.White, Color.Black, 1);
                        else if (ReadyTextStyle == ReadyTextStyleOptions.AlternatingColor)
                            overlayFont.DrawTextWithContrast(ReadyText, icon.Pos + readyOffset, ReadyTextAltColor, Color.Black, 1);
                    }
                    else if (first.Paused)
                        overlayFont.DrawTextWithContrast(HoldText,
                            icon.Pos + holdOffset,
                            Color.White, Color.Black, 1);
                    else if (!waiting && DrawTime)
                        overlayFont.DrawTextWithContrast(WidgetUtils.FormatTime(first.Queue.RemainingTimeActual(first), World.Timestep),
                            icon.Pos + timeOffset,
                            Color.White, Color.Black, 1);

                    if (first.Infinite)
                        symbolFont.DrawTextWithContrast(InfiniteSymbol,
                            icon.Pos + infiniteOffset,
                            Color.White, Color.Black, 1);
                    else if (total > 1 || waiting)
                        overlayFont.DrawTextWithContrast(total.ToString(),
                            icon.Pos + queuedOffset,
                            Color.White, Color.Black, 1);
                }
            }
        }

        private void UpdatePosition()
        {
            var displayPosition = worldRenderer.Viewport.WorldToViewPx(worldRenderer.ScreenPxPosition(factoryActor.CenterPosition));

            Bounds.X = displayPosition.X + (int)Math.Round(factoryActor.Trait<IDecorationBounds>()
                                                               .DecorationBounds(factoryActor, worldRenderer).Width * worldRenderer.Viewport.Zoom) - Bounds.Width / 2;
            Bounds.Y = displayPosition.Y - Bounds.Height / 2;
        }

        public override string GetCursor(int2 pos)
        {
            var icon = icons.Where(i => i.Key.Contains(pos))
                .Select(i => i.Value).FirstOrDefault();

            return icon != null ? base.GetCursor(pos) : null;
        }
    }
}