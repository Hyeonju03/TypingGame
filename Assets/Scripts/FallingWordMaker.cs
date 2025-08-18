using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;

public class FallingWordMaker : MonoBehaviour
{
    [Header("Refs")]
    public InputManager inputManager;
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

    private readonly Queue<int> recentLanes = new();
    private float laneWidth;

    private void Awake()
    {
        if (spawnParent == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas) spawnParent = canvas.transform;
        }

        laneWidth = (spawnParent.GetComponent<RectTransform>().rect.width - edgePadding * 2) / laneCount;
    }

    public void MakeFallingWord(string token) => Spawn(token);

    public void MakeFallingWord(string word, string sentence)
        => Spawn(!string.IsNullOrWhiteSpace(sentence) ? sentence : word);

    private void Spawn(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();

        GameObject obj = Instantiate(wordPrefab, spawnParent);
        RectTransform root = obj.GetComponent<RectTransform>();

        root.pivot = new Vector2(0, 1);
        root.anchorMin = new Vector2(0, 1);
        root.anchorMax = new Vector2(0, 1);

        var label = obj.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.fontSize = fontSize;
            ApplyOutline(label);

            label.text = text.Replace('^', ' ');
            label.alignment = TextAlignmentOptions.TopLeft;

            label.enableWordWrapping = true;

            var sizeFitter = obj.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
            {
                sizeFitter.SetLayoutVertical();
                sizeFitter.SetLayoutHorizontal();
            }

            if (inputManager != null)
            {
                inputManager.AddWordAndObject(text, obj);
            }
        }

        int laneIdx = -1;
        var availableLanes = Enumerable.Range(0, laneCount).Except(recentLanes);
        if (availableLanes.Any())
        {
            laneIdx = availableLanes.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
        }
        else
        {
            laneIdx = UnityEngine.Random.Range(0, laneCount);
        }

        recentLanes.Enqueue(laneIdx);
        while (recentLanes.Count > laneCount - 1) recentLanes.Dequeue();

        float fullW = spawnParent.GetComponent<RectTransform>().rect.width;
        float startX = (fullW - laneWidth * laneCount) / 2f + laneWidth * laneIdx;
        root.anchoredPosition = new Vector2(startX + 10f, spawnParent.GetComponent<RectTransform>().rect.height / 2f + startYOffset);

        root.sizeDelta = new Vector2(laneWidth - 20, root.sizeDelta.y);

        float killY = -spawnParent.GetComponent<RectTransform>().rect.height - bottomExtra;
        StartCoroutine(FallDown(root, obj, killY));
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

        if (inputManager != null)
        {
            inputManager.RemoveWordAndObject(obj);
        }
        Destroy(obj);
    }
}