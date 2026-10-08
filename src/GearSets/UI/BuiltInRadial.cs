using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GearSets.Core;
using GearSets.Core.Model;
using UnityEngine;
using Valheim.UI;
using Object = UnityEngine.Object;

namespace GearSets.UI
{
    /// <summary>
    /// The set picker inside Valheim's own radial menu (Hud.m_radialMenu): the radial key opens it
    /// straight onto the sets, and the main radial (G, gamepad) gets a "Gear Sets" group. Each set is
    /// a vanilla emote element (icon on the ring, name and status in the centre). If the built-in
    /// radial ever fails, Broken is set and the Classic picker (RadialPicker) is used instead.
    /// </summary>
    internal static class BuiltInRadial
    {
        internal static readonly GearSetsRadialConfig SetsConfig = new GearSetsRadialConfig();
        private static readonly ConditionalWeakTable<RadialMenuElement, string> SetIds = new ConditionalWeakTable<RadialMenuElement, string>();
        private static GearSetsOpenConfig _opener;
        private static float _pressedAt = -1f;
        private static int _openFrame = -1;
        private static int _closedFrame = -1;

        /// <summary>The built-in radial failed this session; the key uses Classic and the group is hidden.</summary>
        public static bool Broken { get; private set; }

        public static bool UseForKey
        {
            get { return !Broken && PluginConfig.RadialStyle.Value == RadialStyle.BuiltIn; }
        }

        private static bool ShowGroup
        {
            get { return !Broken && PluginConfig.Enabled.Value && PluginConfig.ShowInMainRadial.Value; }
        }

        /// <summary>The radial key closed the radial this frame, so the same press must not reopen it.</summary>
        public static bool JustClosedByKey
        {
            get { return Time.frameCount == _closedFrame; }
        }

        /// <summary>
        /// Open Valheim's radial on the sets, as the vanilla radial key would. False if it failed
        /// (now Broken) or vanilla declined to open (building tool out); the caller uses Classic.
        /// </summary>
        public static bool TryOpen()
        {
            try
            {
                RadialBase radial = Hud.instance != null ? Hud.instance.m_radialMenu : null;
                if (radial == null)
                    throw new InvalidOperationException("the HUD has no radial menu");
                if (_opener == null)
                {
                    _opener = ScriptableObject.CreateInstance<GearSetsOpenConfig>();
                    if (_opener == null)
                        throw new InvalidOperationException("could not create the radial opener");
                    _opener.hideFlags = HideFlags.HideAndDontSave;
                }
                _pressedAt = Time.unscaledTime;
                _openFrame = Time.frameCount;
                SetsConfig.Failed = false;
                // Vanilla sets CanOpen when the radial button goes down (Player.HandleRadialInput).
                radial.CanOpen = true;
                radial.Open(_opener);
                if (SetsConfig.Failed)
                    throw new InvalidOperationException("building the set entries failed");
                // Vanilla refuses to open with a building tool out (RadialBase.Open, place mode):
                // not a failure, but this press falls back to the Classic ring.
                return radial.Active;
            }
            catch (Exception e)
            {
                Fail("GearSets: the built-in radial failed; using the Classic radial for this session", e);
                return false;
            }
        }

        internal static void Fail(string key, Exception e)
        {
            GearSetsPlugin.WarnOnce(key, e);
            Broken = true;
            try
            {
                RadialBase radial = Hud.instance != null ? Hud.instance.m_radialMenu : null;
                if (radial != null && radial.Active)
                    radial.QueuedClose();
            }
            catch (Exception e2)
            {
                GearSetsPlugin.WarnOnce("GearSets: closing the built-in radial failed", e2);
            }
        }

        /// <summary>Close the radial if it is showing the sets (the mod was switched off).</summary>
        public static void CloseIfOpen()
        {
            if (Hud.instance == null)
                return;
            RadialBase radial = Hud.instance.m_radialMenu;
            if (radial != null && radial.Active && radial.CurrentConfig == SetsConfig)
                radial.QueuedClose();
        }

        /// <summary>
        /// The vanilla controls (RadialConfigHelper.SetItemInteractionControls) only know the
        /// OpenRadial/OpenEmote buttons. Add the same rules for our key: pressing it again closes,
        /// and releasing it after HoldCloseDelay uses the hovered set (release-to-use on) or closes.
        /// </summary>
        internal static void WrapControls(RadialBase radial)
        {
            Func<bool> release = radial.GetReleaseToUse;
            radial.GetReleaseToUse = () => (release != null && release()) || (ReleaseToUse() && HeldAndReleased());
            Func<bool> close = radial.GetClose;
            radial.GetClose = () => (close != null && close()) || PressedAgain() || (!ReleaseToUse() && HeldAndReleased());
        }

        private static bool ReleaseToUse()
        {
            return RadialData.SO != null && RadialData.SO.EnableReleaseToUseMode;
        }

        private static bool HeldAndReleased()
        {
            try
            {
                if (_pressedAt < 0f || RadialData.SO == null || !Keys.Up(PluginConfig.RadialKey.Value))
                    return false;
                bool held = Time.unscaledTime - _pressedAt > RadialData.SO.HoldCloseDelay;
                _pressedAt = -1f;
                return held;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: radial key release check failed", e);
                return false;
            }
        }

        private static bool PressedAgain()
        {
            try
            {
                if (Time.frameCount == _openFrame || !Keys.Down(PluginConfig.RadialKey.Value))
                    return false;
                _closedFrame = Time.frameCount;
                _pressedAt = -1f;
                return true;
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: radial key press check failed", e);
                return false;
            }
        }

        /// <summary>
        /// Called before the main radial is laid out: refresh a set that is the "last used" entry
        /// and insert the Gear Sets group after the vanilla groups.
        /// </summary>
        internal static void AddToMain(RadialBase radial, List<RadialMenuElement> elements)
        {
            try
            {
                RefreshLastUsed(elements);
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: refreshing the last used set in the radial failed", e);
            }
            if (!ShowGroup || RadialData.SO == null || RadialData.SO.GroupElement == null)
                return;
            GroupElement group = Object.Instantiate(RadialData.SO.GroupElement);
            try
            {
                group.Init(SetsConfig, radial.CurrentConfig, radial);
            }
            catch
            {
                Object.Destroy(group.gameObject);
                throw;
            }
            group.gameObject.name = "GearSetsGroup";
            int at = elements.Count;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                if (elements[i] is GroupElement && !(elements[i] is BackElement))
                {
                    at = i + 1;
                    break;
                }
            }
            elements.Insert(at, group);
            // Nine entries make Valheim use its 12-slot ring and re-centre it on element 0, which
            // scrambles the vanilla order; pre-order so the hotbar stays on top and the rest follow
            // clockwise as usual, with the free slots at the bottom.
            int[] order = RadialOrder.TopLevel(elements.Count, RadialData.SO.MaxElementsRange);
            var copy = new List<RadialMenuElement>(elements);
            for (int i = 0; i < order.Length; i++)
                elements[i] = copy[order[i]];
        }

        private static void RefreshLastUsed(List<RadialMenuElement> elements)
        {
            Snapshot snap = null;
            foreach (RadialMenuElement e in elements)
            {
                string id;
                if (e == null || !SetIds.TryGetValue(e, out id))
                    continue;
                GearSet set = Find(id);
                if (set == null)
                {
                    e.SubTitle = "No longer exists";
                    e.Description = "";
                    continue;
                }
                if (Player.m_localPlayer == null)
                    return;
                if (snap == null)
                    snap = Snapshotter.Take(Player.m_localPlayer);
                Fill(e, set, snap);
            }
        }

        /// <param name="topLevel">Opened by the radial key (no back button on the page).</param>
        internal static void AddEntries(List<RadialMenuElement> list, bool topLevel)
        {
            Player p = Player.m_localPlayer;
            IList<GearSet> sets = SetStore.Book.Sets;
            if (p == null || sets.Count == 0)
            {
                EmptyElement empty = Object.Instantiate(RadialData.SO.EmptyElement);
                list.Add(empty);
                empty.Init();
                empty.Name = "No gear sets yet";
                empty.SubTitle = "Save one from the Gear Sets tab in the inventory.";
                return;
            }
            Snapshot snap = Snapshotter.Take(p);
            int[] order = topLevel ? RadialOrder.TopLevel(sets.Count, RadialData.SO.MaxElementsRange) : null;
            for (int i = 0; i < sets.Count; i++)
            {
                GearSet set = sets[order != null ? order[i] : i];
                EmoteElement e = Object.Instantiate(RadialData.SO.EmoteElement);
                list.Add(e);
                Fill(e, set, snap);
            }
        }

        private static void Fill(RadialMenuElement e, GearSet set, Snapshot snap)
        {
            SwapPlan plan = SwapPlanner.Plan(set, snap.Facts);
            HotbarPlan hot = set.HasHotbar ? HotbarPlanner.Plan(set, snap.Facts) : null;
            RadialText.Entry text = RadialText.For(plan, hot, Loc.T);
            e.Name = "<noparse>" + set.Name + "</noparse>";
            // Valheim's centre text shows only Name and SubTitle for non-item entries, so the detail
            // goes on a smaller second line; Description is filled too for anything that reads it.
            e.SubTitle = Status(text) + "\n<size=75%>" + text.Description + "</size>";
            e.Description = text.Description;
            Sprite icon = Icons.For(set.Icon);
            e.Icon.sprite = icon;
            e.Icon.gameObject.SetActive(icon != null);
            string id = set.Id;
            e.Interact = () => Choose(id);
            e.CloseOnInteract = () => true;
            e.gameObject.name = "GearSetsEntry";
            SetIds.Remove(e);
            SetIds.Add(e, id);
        }

        private static string Status(RadialText.Entry text)
        {
            if (text.State == SetState.Equipped)
                return "<color=#" + ColorUtility.ToHtmlStringRGB(UiKit.Equipped) + ">" + text.Subtitle + "</color>";
            if (text.State == SetState.Missing)
                return "<color=#" + ColorUtility.ToHtmlStringRGB(UiKit.Warn) + ">" + text.Subtitle + "</color>";
            return text.Subtitle;
        }

        private static bool Choose(string id)
        {
            try
            {
                if (!PluginConfig.Enabled.Value)
                    return true; // mod switched off: just close
                GearSet set = Find(id);
                if (set == null)
                {
                    Player p = Player.m_localPlayer;
                    if (p != null)
                        p.Message(MessageHud.MessageType.TopLeft, "That gear set no longer exists.");
                }
                else
                {
                    SwapExecutor.Equip(set);
                }
            }
            catch (Exception e)
            {
                GearSetsPlugin.WarnOnce("GearSets: equipping from the radial failed", e);
            }
            return true; // close the radial either way
        }

        private static GearSet Find(string id)
        {
            foreach (GearSet s in SetStore.Book.Sets)
                if (s.Id == id)
                    return s;
            return null;
        }

        internal static Sprite GroupIcon()
        {
            IList<GearSet> sets = SetStore.Book.Sets;
            Sprite s = sets.Count > 0 ? Icons.For(sets[0].Icon) : null;
            return s != null ? s : Icons.For("HelmetBronze");
        }
    }

    /// <summary>The page of set entries: opened by the radial key, or from the Gear Sets group.</summary>
    internal sealed class GearSetsRadialConfig : IRadialConfig
    {
        /// <summary>Set when building the entries failed, so TryOpen can fall back to Classic.</summary>
        public bool Failed;

        public string LocalizedName
        {
            get { return "Gear Sets"; }
        }

        public Sprite Sprite
        {
            get
            {
                try
                {
                    return BuiltInRadial.GroupIcon();
                }
                catch (Exception e)
                {
                    GearSetsPlugin.WarnOnce("GearSets: radial group icon failed", e);
                    return null;
                }
            }
        }

        /// <summary>Runs inside RadialBase.Open/Update, so it never throws into vanilla.</summary>
        public void InitRadialConfig(RadialBase radial)
        {
            var list = new List<RadialMenuElement>();
            try
            {
                BuiltInRadial.AddEntries(list, radial.IsTopLevel && !radial.IsHoverMenu);
                radial.ConstructRadial(list);
            }
            catch (Exception e)
            {
                Failed = true;
                foreach (RadialMenuElement el in list)
                    if (el != null && el.transform.parent == null)
                        Object.Destroy(el.gameObject);
                BuiltInRadial.Fail("GearSets: building the radial entries failed; using the Classic radial for this session", e);
            }
        }
    }

    /// <summary>
    /// Opens the radial like Hud.m_config does. RadialBase.Open only loads the controls and resets
    /// its state for an OpenRadialConfig, so this derives from it and re-implements IRadialConfig
    /// to open the sets page instead of the main page.
    /// </summary>
    public sealed class GearSetsOpenConfig : OpenRadialConfig, IRadialConfig
    {
        string IRadialConfig.LocalizedName
        {
            get { return "Gear Sets"; }
        }

        Sprite IRadialConfig.Sprite
        {
            get { return null; }
        }

        void IRadialConfig.InitRadialConfig(RadialBase radial)
        {
            // As OpenRadialConfig.InitRadialConfig, then our key's controls, then the sets page.
            radial.OnInteractionDelay = delay => PlayerController.SetTakeInputDelay(delay);
            radial.ShouldAnimateIn = true;
            BuiltInRadial.WrapControls(radial);
            radial.Open(BuiltInRadial.SetsConfig);
        }
    }
}
