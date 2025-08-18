// FallingWordMaker.cs — 단일 TMP로 단어/문장 표시(문장 ^→공백 치환, 줄바꿈/높이 자동)
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FallingWordMaker : MonoBehaviour
{
    [Header("Prefab & Canvas")]
    public GameObject wordPrefab;   // TMP_Text 1개만 포함된 프리팹
    public Transform spawnParent;   // Canvas(=RectTransform)

    [Header("Spawn Settings")]
    public int laneCount = 5;
    public float edgePadding = 80f;
    public float startYOffset = 40f;
    public float outlineWidth = 0.2f;
    public Color outlineColor = Color.red;

    [Header("Fall Settings")]
    public float fallSpeed = 160f;
    public float bottomExtra = 80f;

    private readonly Queue<int> recentLanes = new();

    private void Awake()
    {
        if (spawnParent == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas) spawnParent = canvas.transform;
        }
    }

    // 단일 문자열 버전(권장: ReWordSpawner가 token만 넘김)
    public void MakeFallingWord(string token) => Spawn(token);

    // 호환용: (word, sentence) 중 비어있지 않은 쪽 사용
    public void MakeFallingWord(string word, string sentence)
        => Spawn(!string.IsNullOrWhiteSpace(sentence) ? sentence : word);

    private void Spawn(string token)
    {
        if (spawnParent == null || wordPrefab == null) return;

        var canvasRect = spawnParent.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        float startY = halfH + startYOffset;

        // 레인 폭 계산
        float usableW = (halfW - edgePadding) - (-halfW + edgePadding);
        float laneWidth = usableW / Mathf.Max(1, laneCount);
        float xLeft = -halfW + edgePadding;

        // 최근 사용 레인 제외
        List<int> candidates = new();
        for (int i = 0; i < laneCount; i++) if (!recentLanes.Contains(i)) candidates.Add(i);
        if (candidates.Count == 0) { recentLanes.Clear(); for (int i = 0; i < laneCount; i++) candidates.Add(i); }

        int laneIdx = candidates[Random.Range(0, candidates.Count)];
        float xCenter = xLeft + (laneIdx + 0.5f) * laneWidth;

        // 프리팹 생성/배치
        var obj = Instantiate(wordPrefab, spawnParent);
        var root = obj.GetComponent<RectTransform>();
        root.anchoredPosition = new Vector2(xCenter, startY);

        // TMP 하나만 사용
        var label = obj.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            // ^ → 공백 치환 후 세팅
            string text = (token ?? string.Empty).Replace("^", " ").Trim();
            label.text = text;

            // 외곽선
            ApplyOutline(label);

            // 줄바꿈/오토사이즈 강제
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 24;
            label.alignment = TextAlignmentOptions.TopLeft;

            // 레인 폭에 맞춰 가로폭 고정
            var lRect = label.GetComponent<RectTransform>();
            lRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(120f, laneWidth - 20f));

            // 내용 기준으로 세로 높이 확장(루트도 함께 확장해 잘림 방지)
            label.ForceMeshUpdate();
            float h = Mathf.Ceil(label.preferredHeight) + 8f; // 패딩
            lRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        }

        // 최근 레인 기록
        recentLanes.Enqueue(laneIdx);
        while (recentLanes.Count > laneCount - 1) recentLanes.Dequeue();

        // 낙하
        StartCoroutine(FallDown(root, obj, -halfH - bottomExtra));
    }

    private void ApplyOutline(TMP_Text label)
    {
        var mat = label.fontMaterial;
        TMPro.ShaderUtilities.GetShaderPropertyIDs();
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, outlineWidth);
        mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, outlineColor);
        label.fontMaterial = mat;
    }

    private IEnumerator FallDown(RectTransform rect, GameObject obj, float killY)
    {
        while (rect.anchoredPosition.y > killY)
        {
            rect.anchoredPosition -= new Vector2(0, fallSpeed * Time.deltaTime);
            yield return null;
        }
        Destroy(obj);
    }
}
