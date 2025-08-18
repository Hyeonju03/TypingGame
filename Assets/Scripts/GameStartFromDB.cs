// GameStartFromDB.cs (단일 API /api/learn/both 사용, 단어+문장 동시 수신)
// - 해설 필드는 받아오되, 떨어질 때는 munjang만 사용
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

// ✅ Unity용 수신 DTO (Java 쪽에 통합 DTO 없어도 됨)
[System.Serializable]
public class BothDTO
{
    public string vocabulary;          // 단어
    public string descriptionWord;     // 단어 해설(사용 안 함)
    public string munjang;             // 문장
    public string descriptionMunjang;  // 문장 해설(사용 안 함)
}

// 배열 대응용 래퍼 (혹시 배열로 내려올 때 대비)
[System.Serializable] class BothDTOArray { public BothDTO[] items; }

public class GameStartFromDB : MonoBehaviour
{
    [SerializeField] private FallingWordMaker maker;

    [Header("API (단일)")]
    [SerializeField] private string bothUrl = "http://localhost:9001/api/learn/both";

    [Header("Initial Spawn")]
    [SerializeField] private int initialSpawnCount = 0;        // 시작 시 생성 개수
    [SerializeField] private float initialSpawnInterval = 0.15f; // 생성 간격

    // 🚀 시작 시 지정 개수만큼 단어+문장을 받아 스폰
    private IEnumerator Start()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            BothDTO both = null;
            yield return StartCoroutine(GetBoth(r => both = r));

            if (both != null && !string.IsNullOrEmpty(both.vocabulary))
            {
                // ✅ 단어 + 문장 전달 (해설은 사용 안 함)
                maker.MakeFallingWord(both.vocabulary, both.munjang ?? "");
            }

            if (initialSpawnInterval > 0f)
                yield return new WaitForSeconds(initialSpawnInterval);
        }
        yield break; // 반복 스폰은 다른 스크립트에서 담당
    }

    // 📌 /api/learn/both 호출해서 단어+문장 한 번에 가져오기 (안전 파싱 포함)
    private IEnumerator GetBoth(System.Action<BothDTO> cb)
    {
        using (var req = UnityWebRequest.Get(bothUrl))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) { cb(null); yield break; }

            string raw = (req.downloadHandler.text ?? "")
                         .TrimStart('\uFEFF', '\u200B').Trim();

            BothDTO dto = null;
            try
            {
                if (raw.Length > 0 && raw[0] == '{')
                {
                    dto = JsonUtility.FromJson<BothDTO>(raw); // 객체 JSON
                }
                else if (raw.Length > 0 && raw[0] == '[')
                {
                    // 배열 JSON일 경우 첫 유효 항목 사용
                    var wrapped = "{\"items\":" + raw + "}";
                    var arr = JsonUtility.FromJson<BothDTOArray>(wrapped);
                    if (arr?.items != null)
                    {
                        foreach (var it in arr.items)
                        {
                            if (it != null && !string.IsNullOrEmpty(it.vocabulary)) { dto = it; break; }
                        }
                    }
                }
            }
            catch { dto = null; }

            cb(dto);
        }
    }
}
