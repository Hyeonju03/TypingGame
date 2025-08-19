using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using System.Text.RegularExpressions;

// 리스트 확장 메서드를 별도의 파일에 추가하는 것을 권장합니다.
public static class ListExtensions
{
    public static T GetRandomItem<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return default(T);
        }
        int randomIndex = UnityEngine.Random.Range(0, list.Count);
        return list[randomIndex];
    }
}

public class FallingWordMaker : MonoBehaviour
{
    [Header("Refs")]
    public InputManager inputManager;
    public HealthManager healthManager;
    public GameObject wordPrefab;
    public Transform spawnParent;

    [Header("Spawn Settings")]
    public int laneCount = 5;
    public float edgePadding = 80f;
    public float startYOffset = 40f;
    public float outlineWidth = 0.2f;
    public Color outlineColor = Color.red;
    public float fontSize = 36f;

    [Header("Fall Settings")]
    public float fallSpeed = 160f;
    public float bottomExtra = 80f;

    [Header("Kill Line (숫자값으로만 사용)")]
    public bool useCustomKillLine = true;
    public float killLineY = -120f;
    public RectTransform killLineRect;

    private readonly Queue<int> recentLanes = new();
    private float laneWidth;

    void Awake()
    {
        if (spawnParent == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas) spawnParent = canvas.transform;
        }

        if (spawnParent != null)
        {
            var canvasRect = spawnParent.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                float usableW = (canvasRect.rect.width - edgePadding * 2);
                laneWidth = usableW / Mathf.Max(1, laneCount);
            }
        }
    }

    public GameObject MakeFallingWord(string token) // ✅ GameObject를 반환하도록 수정
    {
        if (spawnParent == null || wordPrefab == null) return null; // ✅ null 반환

        var canvasRect = spawnParent.GetComponent<RectTransform>();
        if (canvasRect == null) return null; // ✅ null 반환

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        float startY = halfH + startYOffset;

        float usableW = (halfW - edgePadding) - (-halfW + edgePadding);
        float laneWidth = usableW / Mathf.Max(1, laneCount);
        float xLeft = -halfW + edgePadding;

        List<int> candidates = new();
        for (int i = 0; i < laneCount; i++) if (!recentLanes.Contains(i)) candidates.Add(i);
        if (candidates.Count == 0) { recentLanes.Clear(); for (int i = 0; i < laneCount; i++) candidates.Add(i); }
        int laneIdx = candidates.Count > 0 ? candidates.GetRandomItem() : 0;
        float xCenter = xLeft + (laneIdx + 0.5f) * laneWidth;

        var obj = Instantiate(wordPrefab, spawnParent);
        var root = obj.GetComponent<RectTransform>();

        var label = obj.GetComponentInChildren<TMP_Text>();
        float textWidth = 0f;
        if (label != null)
        {
            label.fontSize = fontSize;
            ApplyOutline(label);
            string textToDisplay = token.Trim();
            label.text = textToDisplay;

            label.alignment = TextAlignmentOptions.TopLeft;
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 40;

            var textRect = label.rectTransform;
            textRect.sizeDelta = new Vector2(laneWidth, textRect.sizeDelta.y);

            var sizeFitter = obj.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
            {
                sizeFitter.SetLayoutVertical();
                sizeFitter.SetLayoutHorizontal();
            }
            label.ForceMeshUpdate();
            textWidth = label.renderedWidth;

            // ✅ InputManager에 대한 직접 호출 코드를 제거
            // inputManager.AddWordAndObject(token.Replace(' ', '^'), obj);
        }

        float halfRootW = root.rect.width / 2f;
        float minX = -halfW + edgePadding + halfRootW;
        float maxX = halfW - edgePadding - halfRootW;
        float clampedX = Mathf.Clamp(xCenter, minX, maxX);
        root.anchoredPosition = new Vector2(clampedX, startY);

        float killY = useCustomKillLine ? killLineY : (-halfH - bottomExtra);
        StartCoroutine(FallDown(rect: root, obj: obj, killY: killY));

        recentLanes.Enqueue(laneIdx);
        while (recentLanes.Count > laneCount - 1) recentLanes.Dequeue();

        return obj; // ✅ 생성된 오브젝트를 반환
    }

    void ApplyOutline(TMP_Text label)
    {
        var mat = label.fontMaterial;
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
        label.fontMaterial = mat;
    }

    IEnumerator FallDown(RectTransform rect, GameObject obj, float killY)
    {
        while (true)
        {
            var p = rect.anchoredPosition;
            p.y -= fallSpeed * Time.deltaTime;
            rect.anchoredPosition = p;

            if (p.y <= killY) break;
            yield return null;
        }

        if (healthManager != null)
        {
            healthManager.TakeDamage();
        }

        if (inputManager != null)
        {
            inputManager.RemoveWordAndObject(obj);
        }
        Destroy(obj);
    }
}