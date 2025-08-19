using UnityEngine;

public class BirdSpawnerLoop : MonoBehaviour
{
    [SerializeField] RectTransform birdPrefab;
    [SerializeField] int count = 4;
    [SerializeField] Vector2 speedRange = new Vector2(22f, 36f);
    [SerializeField] Vector2 yNormRange = new Vector2(0.18f, 0.55f);
    [SerializeField] Vector2 xGapRange = new Vector2(280f, 520f);
    [SerializeField] Vector2 scaleRange = new Vector2(1.0f, 1.6f);

    RectTransform parent; struct Bird { public RectTransform rt; public float speed; }
    Bird[] birds; float w, h; const float margin = 80f;

    void EnsureParent()
    {
        if (parent == null) parent = GetComponent<RectTransform>();
    }

    void Awake() { EnsureParent(); }
    void Start() { EnsureParent(); Build(); }

    void OnRectTransformDimensionsChange()
    {
        EnsureParent();
        if (!isActiveAndEnabled || parent == null) return;
        RecalcSize();
    }

    void RecalcSize()
    {
        EnsureParent();
        if (parent == null) return;
        var r = parent.rect; w = r.width; h = r.height;
        if (w <= 0 || h <= 0) { w = Screen.width; h = Screen.height; }
    }

    void Build()
    {
        RecalcSize();
        if (birdPrefab == null) { Debug.LogError("[BirdSpawner] Bird Prefab ¹ÌÁöÁ¤"); return; }

        int n = Mathf.Max(1, count);
        birds = new Bird[n];
        float x = Random.Range(0f, w);

        for (int i = 0; i < n; i++)
        {
            var inst = Instantiate(birdPrefab, parent);
            var img = inst.GetComponent<UnityEngine.UI.Image>();
            if (img) img.raycastTarget = false;

            float sc = Random.Range(scaleRange.x, scaleRange.y);
            inst.localScale = new Vector3(sc, sc, 1);

            float y = Mathf.Lerp(-h * 0.5f, h * 0.5f, Random.Range(yNormRange.x, yNormRange.y));
            SetPos(inst, x, y);

            birds[i] = new Bird { rt = inst, speed = Random.Range(speedRange.x, speedRange.y) };
            x += Random.Range(xGapRange.x, xGapRange.y);
        }
    }

    void Update()
    {
        if (birds == null || parent == null) return;
        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < birds.Length; i++)
        {
            var b = birds[i];
            var p = b.rt.anchoredPosition;
            p.x -= b.speed * dt;

            if (p.x < -w * 0.5f - margin)
            {
                p.x = w * 0.5f + margin + Random.Range(0f, xGapRange.y);
                p.y = Mathf.Round(Mathf.Lerp(-h * 0.5f, h * 0.5f, Random.Range(yNormRange.x, yNormRange.y)));
                b.speed = Random.Range(speedRange.x, speedRange.y);
            }

            SetPos(b.rt, p.x, p.y);
            birds[i] = b;
        }
    }

    static void SetPos(RectTransform rt, float x, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
    }
}
