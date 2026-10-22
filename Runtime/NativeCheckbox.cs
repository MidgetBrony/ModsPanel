using SteamShelf.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsPanel
{
    internal static class NativeCheckbox
    {
        internal static Toggle Create(Transform parent)
        {
            foreach (UI_BoolSettingDisplay display in Resources.FindObjectsOfTypeAll<UI_BoolSettingDisplay>())
            {
                if (display == null || !display.gameObject.scene.IsValid()) continue;
                Toggle template = display.GetComponentInChildren<Toggle>(true);
                if (template == null) continue;
                Toggle toggle = UnityEngine.Object.Instantiate(template, parent, false);
                toggle.name = "ModsPanel Native Checkbox";
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.group = null;
                toggle.interactable = true;
                toggle.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                toggle.gameObject.SetActive(true);
                return toggle;
            }
            return null;
        }
    }
}

