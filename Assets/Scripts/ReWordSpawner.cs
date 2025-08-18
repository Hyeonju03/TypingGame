// ReWordSpawner.cs — /api/learn/both 주기 스폰 + stage 분기 (1=단어, 2=문장, 3=둘중 하나만 랜덤)
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ReWordSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FallingWordMaker maker;

    [Header("Spawn Loop")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private bool preventImmediateDup = true;

    [Header("API")]
    [SerializeField] private string url = "http://localhost:9001/api/learn/both";

    [Header("Stage")]
    [SerializeField, Range(1, 3)] private int stage = 3; // 1=단어, 2=문장, 3=둘 중 하나만 랜덤

    [SerializeField] private float retryDelay = 0.6f;

    private Coroutine loop;
    private string lastWord; // 직전 스폰 텍스트(중복 방지)

    private void Awake()
    {
        if (maker == null)
        {
            maker = FindObjectOfType<FallingWordMaker>();
            Debug.LogWarning($"[ReWordSpawner] maker가 비어 있어 FindObjectOfType로 주입: {maker}");
        }
    }

    private void Start()
    {
        loop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;

            string requestUrl = $"{url}?stage={stage}";
            BothDTO dto = null;
            yield return StartCoroutine(GetBoth(requestUrl, r => dto = r));

            if (dto == null) continue;

            // 정제
            string word = (dto.vocabulary ?? "").Replace("^", " ").Trim();
            string sent = ((dto.munjangDisplay ?? dto.munjang) ?? "").Replace("^", " ").Trim();

            // stage별로 "한 개"만 선택
            string token =
                (stage == 1) ? word :
                (stage == 2) ? sent :
                PickOne(word, sent); // stage == 3

            if (string.IsNullOrWhiteSpace(token)) continue;
            if (preventImmediateDup && token == lastWord) continue;

            lastWord = token;
            maker.MakeFallingWord(token); // ✅ 단 한 번만 스폰
        }
    }

    private string PickOne(string w, string s)
    {
        bool hasW = !string.IsNullOrWhiteSpace(w);
        bool hasS = !string.IsNullOrWhiteSpace(s);
        if (!hasW && !hasS) return null;
        if (!hasW) return s;
        if (!hasS) return w;
        return (Random.value < 0.5f) ? w : s; // 50:50
    }

    private IEnumerator GetBoth(string requestUrl, System.Action<BothDTO> done)
    {
        using (var req = UnityWebRequest.Get(requestUrl))
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

            if (string.IsNullOrEmpty(raw)) { done(null); yield break; }
            if (raw[0] != '{') { done(null); yield break; }

            BothDTO dto = null;
            try { dto = JsonUtility.FromJson<BothDTO>(raw); }
            catch { dto = null; }

            done(dto);
        }
    }

    public void Pause()
    {
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    public void Resume()
    {
        if (loop == null) { loop = StartCoroutine(SpawnLoop()); }
    }
}

