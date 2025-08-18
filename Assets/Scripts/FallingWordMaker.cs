using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FallingWordMaker : MonoBehaviour
{
    [Header("Prefab & Canvas")]
    public GameObject wordPrefab;          // TMP_Text만 포함된 프리팹
    public Transform spawnParent;           // Canvas(=RectTransform)

    [Header("Spawn Settings")]
    public int laneCount = 5;
    public float edgePadding = 80f;
    public float startYOffset = 40f;
    public float outlineWidth = 0.2f;
    public Color outlineColor = Color.red;

    [Header("Fall Settings")]
    public float fallSpeed = 160f;          // px/sec
    public float bottomExtra = 80f;         // KillLineY 미사용 시 화면 아래 여유

    [Header("Kill Line (숫자값으로만 사용)")]
    public bool useCustomKillLine = true;   // ✅ 라인 오브젝트 없이 숫자값만 사용
    public float killLineY = -120f;         // ✅ 캔버스 로컬Y(원하는 삭제 높이)
    public RectTransform killLineRect;      // ❌ 비워두기

    private readonly Queue<int> recentLanes = new();

    void Awake()
    {
        if (spawnParent == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas) spawnParent = canvas.transform;
        }
    }

    public void MakeFallingWord(string token) => Spawn(token);

    void Spawn(string token)
    {
        if (spawnParent == null || wordPrefab == null) return;

        var canvasRect = spawnParent.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        float startY = halfH + startYOffset;

        // 레인 계산
        float usableW = (halfW - edgePadding) - (-halfW + edgePadding);
        float laneWidth = usableW / Mathf.Max(1, laneCount);
        float xLeft = -halfW + edgePadding;

        // 최근 레인 제외
        List<int> candidates = new();
        for (int i = 0; i < laneCount; i++) if (!recentLanes.Contains(i)) candidates.Add(i);
        if (candidates.Count == 0) { recentLanes.Clear(); for (int i = 0; i < laneCount; i++) candidates.Add(i); }
        int laneIdx = candidates[Random.Range(0, candidates.Count)];
        float xCenter = xLeft + (laneIdx + 0.5f) * laneWidth;

        // 생성/배치 (UI 좌표)
        var obj = Instantiate(wordPrefab, spawnParent);
        var root = obj.GetComponent<RectTransform>();
        root.anchoredPosition = new Vector2(xCenter, startY);

        // 텍스트 세팅
        var label = obj.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            string text = (token ?? string.Empty).Replace("^", " ").Trim();
            label.text = text;

            ApplyOutline(label);
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12; label.fontSizeMax = 24;
            label.alignment = TextAlignmentOptions.TopLeft;

            var lRect = label.GetComponent<RectTransform>();
            float targetWidth = Mathf.Max(120f, laneWidth - 20f);
            lRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

            label.ForceMeshUpdate();
            float h = Mathf.Ceil(label.preferredHeight) + 8f;
            lRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        }

        // ✅ 숫자 KillLineY 사용 (라인 오브젝트 없이)
        float killY = useCustomKillLine
            ? killLineY
            : (-halfH - bottomExtra); // 필요 시 화면 아래로 폴백

        StartCoroutine(FallDown(rect: root, obj: obj, killY: killY));

        recentLanes.Enqueue(laneIdx);
        while (recentLanes.Count > laneCount - 1) recentLanes.Dequeue();
    }

    void ApplyOutline(TMP_Text label)
    {
        var mat = label.fontMaterial;
        TMPro.ShaderUtilities.GetShaderPropertyIDs();
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, outlineWidth);
        mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, outlineColor);
        label.fontMaterial = mat;
    }

    IEnumerator FallDown(RectTransform rect, GameObject obj, float killY)
    {
        while (true)
        {
            var p = rect.anchoredPosition;
            p.y -= fallSpeed * Time.deltaTime;
            rect.anchoredPosition = p;

            // 🔻 텍스트 "하단" Y (pivot/높이 반영)
            float textBottom = p.y - rect.pivot.y * rect.rect.height;

            if (textBottom <= killY) break;
            yield return null;
        }
        Destroy(obj);
    }
}
