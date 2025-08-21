// UIDropBounce.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class DropBounce : MonoBehaviour
{
    [Header("Drop")]
    [SerializeField] float dropDistance = 600f;   // 시작 높이(위로 +)
    [SerializeField] float dropDuration = 0.6f;   // 내려오는 시간

    [Header("Bounce")]
    [SerializeField] int bounceCount = 2;     // 튕김 횟수
    [SerializeField] float bounceHeight = 60f;   // 첫 튕김 높이(+Y)
    [SerializeField] float bounceDuration = 0.25f; // 첫 튕김 왕복 시간
    [SerializeField, Range(0f, 1f)] float damping = 0.5f; // 매 튕김마다 감소율

    [Header("Squash (착지 찰칵 효과)")]
    [SerializeField] bool squash = true;
    [SerializeField] float squashAmount = 0.12f;  // X 늘리고 Y 줄이기
    [SerializeField] float squashTime = 0.12f;

    [SerializeField] bool useUnscaledTime = true; // 일시정지 중에도 동작

    RectTransform rt;
    Vector2 endPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        endPos = rt.anchoredPosition; // 최종 위치 기록
    }

    void OnEnable()
    {
        // 매번 켜질 때 애니메이션 재생
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        // 시작 위치: 위쪽으로 dropDistance 만큼 올려서 시작
        Vector2 startPos = endPos + new Vector2(0f, dropDistance);
        rt.anchoredPosition = startPos;
        rt.localScale = Vector3.one;

        // 1) 낙하 (easeOutCubic)
        yield return LerpPos(startPos, endPos, dropDuration, EaseOutCubic);

        // 1-1) 착지 찰칵
        if (squash)
        {
            yield return ScaleTo(new Vector3(1f + squashAmount, 1f - squashAmount, 1f), squashTime * 0.5f);
            yield return ScaleTo(Vector3.one, squashTime * 0.5f);
        }

        // 2) 튕김 반복 (위로 살짝 튀고 다시 내려옴)
        float h = bounceHeight;
        float t = bounceDuration;
        for (int i = 0; i < bounceCount; i++)
        {
            Vector2 up = endPos + new Vector2(0f, h);

            // 올라갈 때는 easeOut, 내려올 때는 easeIn
            yield return LerpPos(endPos, up, t * 0.5f, EaseOutCubic);
            yield return LerpPos(up, endPos, t * 0.5f, EaseInCubic);

            h *= damping;
            t *= Mathf.Lerp(0.7f, 0.9f, damping); // 살짝 빨라지게
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
