// UIDropBounce.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class DropBounce : MonoBehaviour
{
    [Header("Drop")]
    [SerializeField] float dropDistance = 600f;   // ���� ����(���� +)
    [SerializeField] float dropDuration = 0.6f;   // �������� �ð�

    [Header("Bounce")]
    [SerializeField] int bounceCount = 2;     // ƨ�� Ƚ��
    [SerializeField] float bounceHeight = 60f;   // ù ƨ�� ����(+Y)
    [SerializeField] float bounceDuration = 0.25f; // ù ƨ�� �պ� �ð�
    [SerializeField, Range(0f, 1f)] float damping = 0.5f; // �� ƨ�踶�� ������

    [Header("Squash (���� ��Ĭ ȿ��)")]
    [SerializeField] bool squash = true;
    [SerializeField] float squashAmount = 0.12f;  // X �ø��� Y ���̱�
    [SerializeField] float squashTime = 0.12f;

    [SerializeField] bool useUnscaledTime = true; // �Ͻ����� �߿��� ����

    RectTransform rt;
    Vector2 endPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        endPos = rt.anchoredPosition; // ���� ��ġ ���
    }

    void OnEnable()
    {
        // �Ź� ���� �� �ִϸ��̼� ���
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        // ���� ��ġ: �������� dropDistance ��ŭ �÷��� ����
        Vector2 startPos = endPos + new Vector2(0f, dropDistance);
        rt.anchoredPosition = startPos;
        rt.localScale = Vector3.one;

        // 1) ���� (easeOutCubic)
        yield return LerpPos(startPos, endPos, dropDuration, EaseOutCubic);

        // 1-1) ���� ��Ĭ
        if (squash)
        {
            yield return ScaleTo(new Vector3(1f + squashAmount, 1f - squashAmount, 1f), squashTime * 0.5f);
            yield return ScaleTo(Vector3.one, squashTime * 0.5f);
        }

        // 2) ƨ�� �ݺ� (���� ��¦ Ƣ�� �ٽ� ������)
        float h = bounceHeight;
        float t = bounceDuration;
        for (int i = 0; i < bounceCount; i++)
        {
            Vector2 up = endPos + new Vector2(0f, h);

            // �ö� ���� easeOut, ������ ���� easeIn
            yield return LerpPos(endPos, up, t * 0.5f, EaseOutCubic);
            yield return LerpPos(up, endPos, t * 0.5f, EaseInCubic);

            h *= damping;
            t *= Mathf.Lerp(0.7f, 0.9f, damping); // ��¦ ��������
        }
    }

    IEnumerator LerpPos(Vector2 from, Vector2 to, float dur, System.Func<float, float> ease)
    {
        if (dur <= 0f) { rt.anchoredPosition = to; yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(Mathf.Clamp01(t)));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    IEnumerator ScaleTo(Vector3 target, float dur)
    {
        Vector3 from = rt.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            rt.localScale = Vector3.LerpUnclamped(from, target, EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        rt.localScale = target;
    }

    // Eases
    float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    float EaseInCubic(float x) => x * x * x;
}
