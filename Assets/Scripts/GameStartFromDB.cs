// GameStartFromDB.cs (초기 동시 스폰 제거 + 안전 파싱 적용)
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class VocaDTO { public string vocabulary; public string description; }

[System.Serializable]
class VocaDTOArray { public VocaDTO[] items; }

public class GameStartFromDB : MonoBehaviour
{
    [SerializeField] private FallingWordMaker maker;
    [SerializeField] private string url = "http://localhost:9001/api/voca/one";

    [SerializeField] private int initialSpawnCount = 0;    // 시작 시 생성 개수(기본 0)
    [SerializeField] private float initialSpawnInterval = 0.15f;

    private IEnumerator Start()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Accept", "application/json");
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) continue;

                // ▼ 안전 파싱
                string raw = req.downloadHandler.text;
                if (string.IsNullOrEmpty(raw)) { continue; }
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
                        var arr = JsonUtility.FromJson<VocaDTOArray>(wrapped);
                        if (arr?.items != null)
                        {
                            foreach (var it in arr.items)
                                if (it != null && !string.IsNullOrEmpty(it.vocabulary)) { dto = it; break; }
                        }
                    }
                    else if (raw[0] == '"' && raw[raw.Length - 1] == '"')
                    {
                        var word = raw.Substring(1, raw.Length - 2);
                        dto = new VocaDTO { vocabulary = word, description = null };
                    }
                }
                catch { dto = null; }

                if (dto != null && !string.IsNullOrEmpty(dto.vocabulary))
                    maker.MakeFallingWord(dto.vocabulary);
                // ▲ 안전 파싱 끝

                if (initialSpawnInterval > 0f)
                    yield return new WaitForSeconds(initialSpawnInterval);
            }
        }
        yield break; // 반복 스폰은 ReWordSpawner 담당
    }
}
