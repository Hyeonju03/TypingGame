using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text.RegularExpressions;

// WebGL 빌드를 위해 System.Web 대신 직접 파싱 함수를 추가합니다.
public static class UrlExtensions
{
    public static string GetQueryParam(string url, string key)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("file:///"))
        {
            return null;
        }
        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException)
        {
            Debug.LogError("[UrlExtensions] 잘못된 URL 형식: " + url);
            return null;
        }
        var query = uri.Query;
        if (string.IsNullOrEmpty(query)) return null;
        var parameters = query.Substring(1).Split('&');
        foreach (var param in parameters)
        {
            var parts = param.Split('=');
            if (parts.Length == 2 && parts[0] == key)
            {
                return parts[1];
            }
        }
        return null;
    }
}

public enum SpawnMode
{
    Mixed = 3,
    WordOnly = 1,
    SentenceOnly = 2
}

public class ReWordSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FallingWordMaker maker;

    [Header("Spawn Loop")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private bool preventImmediateDup = true;
    [SerializeField] private int initialSpawnCount = 0;
    [SerializeField] private float initialSpawnInterval = 0.15f;

    [Header("Spawn Mode")]
    [SerializeField] public SpawnMode currentMode = SpawnMode.Mixed;

    [Header("API")]
    [SerializeField] private string bothUrl = "http://localhost:9001/api/learn/both";
    [SerializeField] private float retryDelay = 0.6f;

    private Coroutine loop;
    private string lastWord;

    private void Awake()
    {
        if (maker == null)
        {
            maker = FindObjectOfType<FallingWordMaker>();
            Debug.LogWarning($"[ReWordSpawner] maker가 비어 있어 FindObjectOfType로 주입: {maker}");
        }
    }

    private IEnumerator Start()
    {
        string url = Application.absoluteURL;
        string stageParam = UrlExtensions.GetQueryParam(url, "stage");

        if (!string.IsNullOrEmpty(stageParam) && int.TryParse(stageParam, out int stageValue))
        {
            if (Enum.IsDefined(typeof(SpawnMode), stageValue))
            {
                currentMode = (SpawnMode)stageValue;
                Debug.Log($"[ReWordSpawner] URL 파라미터에 따라 SpawnMode가 {currentMode}(Stage {stageValue})로 변경되었습니다.");
            }
        }

        for (int i = 0; i < initialSpawnCount; i++)
        {
            yield return StartCoroutine(SpawnOnce());
            if (initialSpawnInterval > 0f)
            {
                yield return new WaitForSeconds(initialSpawnInterval);
            }
        }

        loop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnOnce()
    {
        string token = null;
        BothDTO dto = null;

        int stage = (int)currentMode;

        yield return StartCoroutine(GetBoth(stage, r => dto = r));

        if (dto != null)
        {
            if (stage == (int)SpawnMode.WordOnly && !string.IsNullOrWhiteSpace(dto.vocabulary))
            {
                token = dto.vocabulary.Trim();
            }
            else if (stage == (int)SpawnMode.SentenceOnly && !string.IsNullOrWhiteSpace(dto.munjangDisplay))
            {
                token = dto.munjangDisplay.Trim();
            }
            else if (stage == (int)SpawnMode.Mixed)
            {
                string word = (dto.vocabulary ?? "").Trim();
                string sent = (dto.munjangDisplay ?? "").Trim();

                bool useSentence = !string.IsNullOrWhiteSpace(sent) && (string.IsNullOrWhiteSpace(word) || UnityEngine.Random.value < 0.5f);
                token = useSentence ? sent : word;
            }
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[ReWordSpawner] 토큰을 가져오지 못했습니다. (네트워크/파싱 실패 또는 빈 응답)");
            yield break;
        }

        if (preventImmediateDup && token == lastWord)
        {
            Debug.Log($"[ReWordSpawner] 직전 텍스트 '{token}'와 동일 → 재시도");
            yield break;
        }
        lastWord = token;

        if (maker == null)
        {
            Debug.LogError("[ReWordSpawner] maker가 null이라 스폰 불가");
            yield break;
        }

        Debug.Log($"[ReWordSpawner] MakeFallingWord 호출: 토큰='{token}'");

        // 1. FallingWordMaker에서 오브젝트를 받아옵니다.
        GameObject newWordObject = maker.MakeFallingWord(token);

        // 2. 받은 오브젝트 정보를 InputManager에 전달합니다.
        if (newWordObject != null && GameManager.Instance.inputManager != null)
        {
            GameManager.Instance.inputManager.AddWordAndObject(token, newWordObject);
            Debug.Log($"[ReWordSpawner] '{token}' 단어를 InputManager에 등록했습니다.");
        }
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;
            yield return StartCoroutine(SpawnOnce());
        }
    }

    private IEnumerator GetBoth(int stage, System.Action<BothDTO> done)
    {
        string fullUrl = $"{bothUrl}?stage={stage}";
        using (var req = UnityWebRequest.Get(fullUrl))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                yield return new WaitForSeconds(retryDelay);
                done(null); yield break;
            }

            string raw = (req.downloadHandler.text ?? "")
                               .TrimStart('\uFEFF', '\u200B', '\u0000').Trim();

            BothDTO dto = null;
            try { dto = JsonUtility.FromJson<BothDTO>(raw); }
            catch (System.Exception ex)
            {
                Debug.LogError("[ReWordSpawner] JSON 파싱 실패: " + ex.Message);
            }

            done(dto);
        }
    }

    public void Pause() { if (loop != null) { StopCoroutine(loop); loop = null; Debug.Log("[ReWordSpawner] Pause"); } }
    public void Resume() { if (loop == null) { loop = StartCoroutine(SpawnLoop()); Debug.Log("[ReWordSpawner] Resume"); } }
}