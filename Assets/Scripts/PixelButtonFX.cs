using UnityEngine;
using UnityEngine.EventSystems;

public class PixelButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] float hoverScale = 1.03f;
    [SerializeField] float pressScale = 0.97f;
    [SerializeField] float lerp = 12f;

    Vector3 baseScale, targetScale;

    void Awake() { baseScale = transform.localScale; targetScale = baseScale; }
    void Update() { transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * lerp); }
    public void OnPointerEnter(PointerEventData _) => targetScale = baseScale * hoverScale;
    public void OnPointerExit(PointerEventData _) => targetScale = baseScale;
    public void OnPointerDown(PointerEventData _) => targetScale = baseScale * pressScale;
    public void OnPointerUp(PointerEventData _) => targetScale = baseScale * hoverScale;
}
