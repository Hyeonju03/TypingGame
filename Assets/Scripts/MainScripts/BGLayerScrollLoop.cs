using UnityEngine;

public class BGLayerScrollLoop : MonoBehaviour
{
    [SerializeField] float speed = 24f;
    [SerializeField] RectTransform cloudA;   // Layer_Clouds�� �ڽ�
    [SerializeField] RectTransform cloudB;

    RectTransform parent;
    float w, h;
    float scrollX; // ���� �̵���

    void Awake()
    {
        parent = (RectTransform)transform;
        if (!cloudA && parent.childCount > 0) cloudA = parent.GetChild(0) as RectTransform;
        if (!cloudB && parent.childCount > 1) cloudB = parent.GetChild(1) as RectTransform;
    }

    void Start() { RefreshLayout(); }

    void OnRectTransformDimensionsChange()
    {
        if (parent) RefreshLayout();
    }

    void RefreshLayout()
    {
        if (!cloudA || !cloudB) { Debug.LogError("[BGLayerScrollLoopUI] CloudA/CloudB ���� �ʿ�"); return; }
        w = parent.rect.width; h = parent.rect.height;
        Setup(cloudA); Setup(cloudB);
        cloudA.anchoredPosition = Vector2.zero;
        cloudB.anchoredPosition = new Vector2(w, 0);
        scrollX = 0f;
    }

    void Setup(RectTransform t)
    {
        t.anchorMin = new Vector2(0f, 0.5f);
        t.anchorMax = new Vector2(0f, 0.5f);
        t.pivot = new Vector2(0f, 0.5f);
        t.sizeDelta = new Vector2(w, h);
    }

    void Update()
    {
        if (!cloudA || !cloudB || w <= 0f) return;

        // ���� �̵���(�·� �帧)
        scrollX += speed * Time.unscaledDeltaTime;

        // w�� �������� ����
        float xA = -(scrollX % w);
        if (xA <= -w) xA += w;

        // �ȼ� ������ "���� �ܰ�"������
        float xB = xA + w;
        cloudA.anchoredPosition = new Vector2(Mathf.Round(xA), 0);
        cloudB.anchoredPosition = new Vector2(Mathf.Round(xB), 0);
    }
}
