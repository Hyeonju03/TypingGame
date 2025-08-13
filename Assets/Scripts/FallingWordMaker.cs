using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FallingWordMaker : MonoBehaviour
{
    [Header("Prefab & Canvas")]
    public GameObject wordPrefab;          // 단어 프리팹
    public Transform spawnParent;          // Canvas(= RectTransform 필수)

    [Header("Spawn Settings")]
    public int laneCount = 5;              // 가로 레인 수 (겹침 방지)
    public float edgePadding = 80f;        // 좌우 여백
    public float startYOffset = 40f;       // 카메라 윗변에서 위로 추가
    public float outlineWidth = 0.2f;      // 테두리 두께(0~1)
    public Color outlineColor = Color.red;

    [Header("Fall Settings")]
    public float fallSpeed = 160f;         // 낙하 속도(px/sec)
    public float bottomExtra = 80f;        // 화면 아래로 더 내려가서 제거


    // 최근 사용 레인(중복 스폰 방지용)
    private readonly Queue<int> recentLanes = new();
    public void MakeFallingWord(string word)
    {
        var canvasRect = spawnParent.GetComponent<RectTransform>();
        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;

        // 윗변 Y 고정
        float startY = halfH + startYOffset;

        // 레인 좌표 계산
        float usableW = (halfW - edgePadding) - (-halfW + edgePadding);          // 가용 폭
        float laneWidth = usableW / Mathf.Max(1, laneCount);
        float xLeft = -halfW + edgePadding;

        // 사용 가능 레인 풀 만들기(최근 사용 레인 제외)
        List<int> candidates = new();
        for (int i = 0; i < laneCount; i++)
            if (!recentLanes.Contains(i)) candidates.Add(i);
        if (candidates.Count == 0) { recentLanes.Clear(); for (int i = 0; i < laneCount; i++) candidates.Add(i); }

        // 랜덤 레인 선택
        int laneIdx = candidates[Random.Range(0, candidates.Count)];
        float xCenter = xLeft + (laneIdx + 0.5f) * laneWidth;

        // 프리팹 생성/배치
        var obj = Instantiate(wordPrefab, spawnParent);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(xCenter, startY);

        // 텍스트 표시 + 테두리 적용
        var label = obj.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.text = word;

            // TMP 머티리얼 Outline 설정
            var mat = label.fontMaterial; // 인스턴스화된 머티리얼
            TMPro.ShaderUtilities.GetShaderPropertyIDs(); // 안전 호출
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, outlineWidth);
            mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, outlineColor);
            label.fontMaterial = mat; // 적용
        }

        // 최근 사용 레인 기록(크기 제한)
        recentLanes.Enqueue(laneIdx);
        while (recentLanes.Count > laneCount - 1) recentLanes.Dequeue();

        // 낙하 시작
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