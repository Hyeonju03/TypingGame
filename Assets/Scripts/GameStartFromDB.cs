// GameStartFromDB.cs — /api/learn/both 사용, 문장만 스폰(표시용 우선)
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class BothDTO
{
    public string vocabulary;          // 단어
    public string descriptionWord;     // 단어 해설(미사용)
    public string munjang;             // 문장(원본)
    public string descriptionMunjang;  // 문장 해설(미사용)
    public string munjangDisplay;      // 문장(표시용) — 있으면 이걸 우선 사용
}

[System.Serializable]
class BothDTOArray { public BothDTO[] items; }

public class GameStartFromDB : MonoBehaviour
{
    [SerializeField] private FallingWordMaker maker;

    [Header("API")]
    [SerializeField] private string bothUrl = "http://localhost:9001/api/learn/both";

    [Header("Initial Spawn")]
    [SerializeField] private int initialSpawnCount = 0;         // 시작 시 생성 개수
    [SerializeField] private float initialSpawnInterval = 0.15f; // 생성 간격(초)

    private IEnumerator Start()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            BothDTO both = null;
            yield return StartCoroutine(GetBoth(r => both = r));
            if (both != null)
            {
                // 문장: 표시용 우선 → 없으면 원본
                var sentence = (both.munjangDisplay ?? both.munjang ?? "").Trim();
                if (!string.IsNullOrEmpty(sentence))
                    maker.MakeFallingWord(sentence); // ✅ 문자열 1개만 전달
            }

            if (initialSpawnInterval > 0f)
                yield return new WaitForSeconds(initialSpawnInterval);
        }
    }

    private IEnumerator GetBoth(System.Action<BothDTO> cb)
    {
        using (var req = UnityWebRequest.Get(bothUrl))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) { cb(null); yield break; }

            string raw = (req.downloadHandler.text ?? "")
                         .TrimStart('\uFEFF', '\u200B', '\u0000')
                         .Trim();

            BothDTO dto = null;
            try
            {
                if (raw.Length > 0 && raw[0] == '{')
                {
                    dto = JsonUtility.FromJson<BothDTO>(raw);
                }
                else if (raw.Length > 0 && raw[0] == '[')
                {
                    var wrapped = "{\"items\":" + raw + "}";
                    var arr = JsonUtility.FromJson<BothDTOArray>(wrapped);
                    if (arr?.items != null)
                    {
                        foreach (var it in arr.items)
                        {
                            if (it != null) { dto = it; break; }
                        }
                    }
                }
            }
            catch { dto = null; }

            cb(dto);
        }
    }
}
