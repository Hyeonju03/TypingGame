using UnityEngine;

// 타이틀에 붙이면 크기와 알파가 심장 뛰듯이 반복됩니다.
[DisallowMultipleComponent]
public class PulseBeatTitle : MonoBehaviour
{
    [Header("리듬")]
    [SerializeField] float bpm = 52f;          // 1분당 박동수(48~60 권장)
    [SerializeField] float phaseOffset = 0f;   // 여러 요소 동기화용(기본 0)

    [Header("크기(배율)")]
    [SerializeField] float scaleMin = 1.00f;
    [SerializeField] float scaleMax = 1.06f;

    [Header("투명도(알파)")]
    [SerializeField] float alphaMin = 0.88f;
    [SerializeField] float alphaMax = 1.00f;

    CanvasGroup cg;
    Vector3 baseScale;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = Time.unscaledTime * (bpm / 60f) + phaseOffset;
        float u = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f; // 0~1
        transform.localScale = baseScale * Mathf.Lerp(scaleMin, scaleMax, u);
        cg.alpha = Mathf.Lerp(alphaMin, alphaMax, u);
    }
}
