using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GearSets.UI
{
    /// <summary>Drag the window by its title bar.</summary>
    internal sealed class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Target;
        public Action Dropped;

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;
            Canvas canvas = Target.GetComponentInParent<Canvas>();
            float sf = canvas != null && canvas.rootCanvas.scaleFactor > 0f ? canvas.rootCanvas.scaleFactor : 1f;
            Target.anchoredPosition += eventData.delta / sf;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Dropped != null)
                Dropped();
        }
    }
}
