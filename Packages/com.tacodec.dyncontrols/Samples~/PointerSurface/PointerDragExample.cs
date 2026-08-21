using DynControls;
using UnityEngine;

public sealed class PointerDragExample : MonoBehaviour
{
    [SerializeField] private PointerSurface _surface;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _draggedRect;

    private bool _isDragging;

    private void OnEnable()
    {
        _surface.Pressed += OnPressed;
        _surface.Released += OnReleased;
    }

    private void OnDisable()
    {
        _surface.Pressed -= OnPressed;
        _surface.Released -= OnReleased;
    }

    private void Update()
    {
        if (!_isDragging || _canvas == null || _draggedRect == null)
            return;

        RectTransform parentRect = _draggedRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Camera canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                _surface.Position,
                canvasCamera,
                out Vector2 localPoint))
        {
            _draggedRect.anchoredPosition = localPoint;
        }
    }

    private void OnPressed(PointerSurface surface)
    {
        _isDragging = true;
    }

    private void OnReleased(PointerSurface surface)
    {
        _isDragging = false;
    }
}
