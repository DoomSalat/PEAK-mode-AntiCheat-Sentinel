using UnityEngine;
using UnityEngine.EventSystems;

namespace PeakSentinel.Reporting.Ui
{
    public sealed class DragHandle : MonoBehaviour, IDragHandler
    {
        private RectTransform _target;
        private Canvas _canvas;

        public void Initialize(RectTransform target, Canvas canvas)
        {
            _target = target;
            _canvas = canvas;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _target.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }
    }
}
