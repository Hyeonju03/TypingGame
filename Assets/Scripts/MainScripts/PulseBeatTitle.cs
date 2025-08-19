using UnityEngine;

// Ÿ��Ʋ�� ���̸� ũ��� ���İ� ���� �ٵ��� �ݺ��˴ϴ�.
[DisallowMultipleComponent]
public class PulseBeatTitle : MonoBehaviour
{
    [Header("����")]
    [SerializeField] float bpm = 52f;          // 1�д� �ڵ���(48~60 ����)
    [SerializeField] float phaseOffset = 0f;   // ���� ��� ����ȭ��(�⺻ 0)

    [Header("ũ��(����)")]
    [SerializeField] float scaleMin = 1.00f;
    [SerializeField] float scaleMax = 1.06f;

    [Header("����(����)")]
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
