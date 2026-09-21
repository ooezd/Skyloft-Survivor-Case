using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,
    IInitializePotentialDragHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField, Min(1f)] private float handleRange = 65f;
    [SerializeField, Range(0f, 0.9f)] private float deadZone = 0.1f;

    public Vector2 Input { get; private set; }

    private int? activePointerId;

    private void OnEnable() => ResetInput();

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || activePointerId.HasValue ||
            eventData.button != PointerEventData.InputButton.Left || background == null || handle == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 point))
            return;

        activePointerId = eventData.pointerId;
        background.anchoredPosition = point;
        background.gameObject.SetActive(true);
        handle.anchoredPosition = Vector2.zero;
        Input = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activePointerId != eventData.pointerId || background == null || handle == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out Vector2 point))
        {
            Vector2 value = Vector2.ClampMagnitude(point / Mathf.Max(1f, handleRange), 1f);
            Input = value.magnitude < deadZone ? Vector2.zero : value;
            handle.anchoredPosition = Input * handleRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (activePointerId == eventData.pointerId)
            ResetInput();
    }

    private void OnDisable() => ResetInput();

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ResetInput();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            ResetInput();
    }

    private void ResetInput()
    {
        activePointerId = null;
        Input = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
        if (background != null)
            background.gameObject.SetActive(false);
    }
}
