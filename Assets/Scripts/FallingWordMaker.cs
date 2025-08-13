using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FallingWordMaker : MonoBehaviour
{
    [Header("Prefab & Canvas")]
    public GameObject wordPrefab;          // �ܾ� ������
    public Transform spawnParent;          // Canvas(= RectTransform �ʼ�)

    [Header("Spawn Settings")]
    public int laneCount = 5;              // ���� ���� �� (��ħ ����)
    public float edgePadding = 80f;        // �¿� ����
    public float startYOffset = 40f;       // ī�޶� �������� ���� �߰�
    public float outlineWidth = 0.2f;      // �׵θ� �β�(0~1)
    public Color outlineColor = Color.red;

    [Header("Fall Settings")]
    public float fallSpeed = 160f;         // ���� �ӵ�(px/sec)
    public float bottomExtra = 80f;        // ȭ�� �Ʒ��� �� �������� ����


    // �ֱ� ��� ����(�ߺ� ���� ������)
    private readonly Queue<int> recentLanes = new();
    public void MakeFallingWord(string word)
    {
        var canvasRect = spawnParent.GetComponent<RectTransform>();
        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;

        // ���� Y ����
        float startY = halfH + startYOffset;

        // ���� ��ǥ ���
        float usableW = (halfW - edgePadding) - (-halfW + edgePadding);          // ���� ��
        float laneWidth = usableW / Mathf.Max(1, laneCount);
        float xLeft = -halfW + edgePadding;

        // ��� ���� ���� Ǯ �����(�ֱ� ��� ���� ����)
        List<int> candidates = new();
        for (int i = 0; i < laneCount; i++)
            if (!recentLanes.Contains(i)) candidates.Add(i);
        if (candidates.Count == 0) { recentLanes.Clear(); for (int i = 0; i < laneCount; i++) candidates.Add(i); }

        // ���� ���� ����
        int laneIdx = candidates[Random.Range(0, candidates.Count)];
        float xCenter = xLeft + (laneIdx + 0.5f) * laneWidth;

        // ������ ����/��ġ
        var obj = Instantiate(wordPrefab, spawnParent);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(xCenter, startY);

        // �ؽ�Ʈ ǥ�� + �׵θ� ����
        var label = obj.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.text = word;

            // TMP ��Ƽ���� Outline ����
            var mat = label.fontMaterial; // �ν��Ͻ�ȭ�� ��Ƽ����
            TMPro.ShaderUtilities.GetShaderPropertyIDs(); // ���� ȣ��
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, outlineWidth);
            mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, outlineColor);
            label.fontMaterial = mat; // ����
        }

        // �ֱ� ��� ���� ���(ũ�� ����)
        recentLanes.Enqueue(laneIdx);
        while (recentLanes.Count > laneCount - 1) recentLanes.Dequeue();

        // ���� ����
        StartCoroutine(FallDown(rect, obj, -halfH - bottomExtra));
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