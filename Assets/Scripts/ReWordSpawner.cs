// ReWordSpawner.cs (주기 스폰 + 안전 파싱 적용)
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
    [SerializeField] private string url = "http://localhost:9001/api/voca/one";
    [SerializeField] private float retryDelay = 0.6f;

    private Coroutine loop;
    private string lastWord;

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

            VocaDTO dto = null;
            yield return StartCoroutine(GetOneWord(r => dto = r));

            if (dto != null && !string.IsNullOrEmpty(dto.vocabulary))
            {
                var word = dto.vocabulary.Trim();
                if (preventImmediateDup && word == lastWord)
                {
                    // 한 번 더 시도
                    VocaDTO retry = null;
                    yield return StartCoroutine(GetOneWord(r => retry = r));
                    if (retry != null && !string.IsNullOrEmpty(retry.vocabulary))
                        word = retry.vocabulary.Trim();
                }
                lastWord = word;
                maker.MakeFallingWord(word);
            }
        }
    }

    private IEnumerator GetOneWord(System.Action<VocaDTO> done)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                yield return new WaitForSeconds(retryDelay);
                done(null); yield break;
            }

            string raw = req.downloadHandler.text;
            if (string.IsNullOrEmpty(raw)) { done(null); yield break; }
            raw = raw.TrimStart('\uFEFF', '\u200B').Trim();

            VocaDTO dto = null;
            try
            {
                if (raw[0] == '{')
                {
                    dto = JsonUtility.FromJson<VocaDTO>(raw);
                }
                else if (raw[0] == '[')
                {
                    string wrapped = "{\"items\":" + raw + "}";
                    var arr = JsonUtility.FromJson<VocaArrayWrapper>(wrapped);
                    if (arr?.items != null)
                    {
                        foreach (var it in arr.items)
                            if (it != null && !string.IsNullOrEmpty(it.vocabulary)) { dto = it; break; }
                    }
                }
                else if (raw[0] == '"' && raw[raw.Length - 1] == '"')
                {
                    var word = raw.Substring(1, raw.Length - 2);
                    dto = new VocaDTO { vocabulary = word };
                }
                // 그 외(HTML/null 등)는 스킵
            }
            catch { dto = null; }

            if (dto == null || string.IsNullOrEmpty(dto.vocabulary)) { done(null); yield break; }
            done(dto);
        }
    }

    [System.Serializable] private class VocaArrayWrapper { public VocaDTO[] items; }

    public void Pause() { if (loop != null) { StopCoroutine(loop); loop = null; } }
    public void Resume() { if (loop == null) loop = StartCoroutine(SpawnLoop()); }
}
