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
}