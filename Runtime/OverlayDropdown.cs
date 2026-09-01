using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModsPanel
{
    internal sealed class OverlayDropdown : TMP_Dropdown
    {
        protected override GameObject CreateDropdownList(GameObject template)
        {
            GameObject list = base.CreateDropdownList(template);
            Canvas popupCanvas = list.GetComponent<Canvas>() ?? list.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 32010;
            if (list.GetComponent<GraphicRaycaster>() == null)
                list.AddComponent<GraphicRaycaster>();
            if (list.GetComponent<DropdownOverlaySorting>() == null)
                list.AddComponent<DropdownOverlaySorting>();
            list.transform.SetAsLastSibling();
            return list;
        }
    }

    internal sealed class DropdownOverlaySorting : MonoBehaviour
    {
        private Canvas popupCanvas;

        private void LateUpdate()
        {
            popupCanvas ??= GetComponent<Canvas>();
            if (popupCanvas == null) return;
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 32010;
        }
    }
}
