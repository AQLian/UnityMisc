using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scripts
{

    public class UnityRectTransformHelper
    {
        //限制目标在rect的范围内
        public static void ClampChildToParent(RectTransform restrictRect, RectTransform relateRect)
        {
            var relateBounds = relateRect.rect;
            var bounds = restrictRect.rect;
            var minX = relateBounds.xMin + bounds.width * restrictRect.pivot.x;
            var maxX = relateBounds.xMax - bounds.width * (1 - restrictRect.pivot.x);
            var minY = relateBounds.yMin + bounds.height * restrictRect.pivot.y;
            var maxY = relateBounds.yMax - bounds.height * (1 - restrictRect.pivot.y);
            var localPosition = relateRect.InverseTransformPoint(restrictRect.position);
            localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);
            localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
            var world = relateRect.TransformPoint(localPosition);
            var relate = restrictRect.parent.InverseTransformPoint(world);
            restrictRect.localPosition = relate;
        }
    }

    public static class UGUIHelper
    {
        public static Vector2 ConvertScreenDeltaToLocalDelta(PointerEventData eventData, RectTransform rect, out bool success)
        {
            if (eventData == null || rect == null)
            {
                success = false;
                return Vector2.zero;
            }
            Vector2 currentScreenPos = eventData.position;
            Vector2 previousScreenPos = currentScreenPos - eventData.delta;
            bool convertedCurrent = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                currentScreenPos,
                eventData.pressEventCamera, // Auto-null for Overlay canvases
                out Vector2 currentLocalPos);
            bool convertedPrevious = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                previousScreenPos,
                eventData.pressEventCamera,
                out Vector2 previousLocalPos);
            success = convertedCurrent && convertedPrevious;
            return success ? currentLocalPos - previousLocalPos : Vector2.zero;
        }

        // convert PointerEventData.delta -> localSpace delta
        // this will make like ondrag event control RectTranform position(.anchoredPosition+=delta)
        // exact match local space coordinate
        public static Vector2 ConvertScreenDeltaToLocalDelta(PointerEventData eventData, RectTransform rect)
        {
            return ConvertScreenDeltaToLocalDelta(eventData, rect, out _);
        }
    }
}