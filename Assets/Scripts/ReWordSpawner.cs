// ReWordSpawner.cs — /api/learn/both 주기 스폰 + 강제 로깅
// (✅ 변경점에 // CHANGED, // NEW 주석 표시)
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
    [SerializeField] private float retryDelay = 0.6f;

    private Coroutine loop;
    private string lastWord; // CHANGED: 토큰(단어/문장)도 여기 재활용

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
        Debug.Log("[ReWordSpawner] Start() 호출 → 스폰 루프 시작");
        loop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;

            Debug.Log("[ReWordSpawner] 루프 틱: /api/learn/both 요청 시작");
            BothDTO dto = null;
            yield return StartCoroutine(GetBoth(r => dto = r));

            if (dto == null)
            {
                Debug.LogWarning("[ReWordSpawner] dto==null (네트워크/파싱 실패 또는 빈 응답)");
                continue;
            }

            Debug.Log($"[ReWordSpawner] 응답 OK: word='{dto.vocabulary}', munjang='{dto.munjang}'");

            // ===== CHANGED: 단어 / 문장 중 하나를 랜덤 선택 =====
            string word = (dto.vocabulary ?? "").Trim();
            string sent = (dto.munjang ?? "").Trim();

            bool useSentence = !string.IsNullOrWhiteSpace(sent) && Random.value < 0.5f;
            string token = useSentence ? sent : word;
            if (string.IsNullOrWhiteSpace(token)) continue;
            // ===== CHANGED 끝 =====

            // ===== CHANGED: 중복 검사도 token 기준 =====
            if (preventImmediateDup && token == lastWord)
            {
                Debug.Log("[ReWordSpawner] 직전 텍스트와 동일 → 재시도");
                BothDTO retry = null;
                yield return StartCoroutine(GetBoth(r => retry = r));
                if (retry != null)
                {
                    word = (retry.vocabulary ?? "").Trim();
                    sent = (retry.munjang ?? "").Trim();
                    useSentence = !string.IsNullOrWhiteSpace(sent) && Random.value < 0.5f;
                    token = useSentence ? sent : word;
                    if (string.IsNullOrWhiteSpace(token)) continue;
                }
            }
            lastWord = token;
            // ===== CHANGED 끝 =====

            if (maker == null)
            {
                Debug.LogError("[ReWordSpawner] maker가 null이라 스폰 불가");
                continue;
            }

            // ===== CHANGED: 한 문자열만 넘김 =====
            Debug.Log($"[ReWordSpawner] MakeFallingWord 호출: {(useSentence ? "문장" : "단어")}='{token}'");
            maker.MakeFallingWord(token);
            // ===== CHANGED 끝 =====
        }
    }


    private IEnumerator GetBoth(System.Action<BothDTO> done)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            Debug.Log($"[ReWordSpawner] HTTP {req.responseCode} result={req.result}");

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[ReWordSpawner] HTTP 실패: " + req.error);
                yield return new WaitForSeconds(retryDelay);
                done(null); yield break;
            }

            string raw = (req.downloadHandler.text ?? "")
                         .TrimStart('\uFEFF', '\u200B', '\u0000').Trim();

            if (string.IsNullOrEmpty(raw))
            {
                Debug.LogWarning("[ReWordSpawner] 빈 본문");
                done(null); yield break;
            }

            int i = 0; while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
            if (i >= raw.Length || raw[i] != '{')
            {
                Debug.LogError("[ReWordSpawner] JSON 객체 아님: " + raw);
                done(null); yield break;
            }

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
