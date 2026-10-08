using UnityEngine;
using UnityEngine.UI;

namespace GearSets.UI
{
    /// <summary>
    /// "Gear Sets" tab on the inventory block's top edge, just left of Larder's tab ("LarderToggle",
    /// 90 wide, anchored top-right) when Larder is installed, else at the top-right corner.
    /// </summary>
    internal static class GearSetsTab
    {
        private const float Gap = 6f;
        private static Button _tab;

        public static void Ensure(InventoryGui gui)
        {
            if (_tab == null && gui != null && gui.m_player != null)
            {
                _tab = UiKit.Button(gui.m_player, "GearSetsTab", gui.m_craftButton, "Gear Sets", GearSetsWindow.Toggle);
                var rt = (RectTransform)_tab.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(110f, 30f);
            }
            Place(gui);
        }

        /// <summary>Cheap; runs every frame while the inventory is open because Larder may build its tab after ours.</summary>
        public static void Place(InventoryGui gui)
        {
            if (_tab == null || gui == null || gui.m_player == null)
                return;
            _tab.gameObject.SetActive(PluginConfig.Enabled.Value);
            var larder = gui.m_player.Find("LarderToggle") as RectTransform;
            float x = larder != null && larder.gameObject.activeSelf ? -(larder.sizeDelta.x + Gap) : 0f;
            ((RectTransform)_tab.transform).anchoredPosition = new Vector2(x, 4f);
            UiKit.SetLabel(_tab, GearSetsWindow.Visible ? "<color=#7ee0c3>Gear Sets</color>" : "Gear Sets", UiKit.TextColor);
        }
    }
}
