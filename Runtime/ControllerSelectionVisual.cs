using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModsPanel
{
    internal sealed class ControllerSelectionVisual : MonoBehaviour
    {
        private Outline focusOutline;

        private void Awake()
        {
            Graphic graphic = GetComponent<Selectable>()?.targetGraphic;
            if (graphic == null) return;

            Shadow shadow = graphic.gameObject.GetComponent<Shadow>() ?? graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(4f, -5f);
            shadow.useGraphicAlpha = true;

            focusOutline = graphic.gameObject.GetComponent<Outline>() ?? graphic.gameObject.AddComponent<Outline>();
            focusOutline.effectColor = new Color32(111, 238, 213, 255);
            focusOutline.effectDistance = new Vector2(5f, 5f);
            focusOutline.useGraphicAlpha = false;
            focusOutline.enabled = false;
        }

        private void Update()
        {
            if (focusOutline != null)
                focusOutline.enabled = EventSystem.current != null &&
                    EventSystem.current.currentSelectedGameObject == gameObject;
        }
    }
}
