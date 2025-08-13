using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class VocaDTO { public string vocabulary; public string description; }

[System.Serializable]
class VocaDTOArray { public VocaDTO[] items; }

public class GameStartFromDB : MonoBehaviour
{
    [SerializeField] private FallingWordMaker maker; // Inspector에 연결
    [SerializeField] private string url = "http://localhost:9001/api/voca/one";

    private IEnumerator Start()
    {
        for (int i = 0; i < 5; i++)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Accept", "application/json");
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                    continue;

                // ★ 변경: 원본 수신 + 형태에 따라 파싱
                string raw = req.downloadHandler.text?.Trim();
                VocaDTO dto = null;

                if (!string.IsNullOrEmpty(raw))
                {
                    if (raw.StartsWith("{")) // 객체
                    {
                        dto = JsonUtility.FromJson<VocaDTO>(raw);
                    }
                    else if (raw.StartsWith("[")) // 배열 → 첫 번째만 사용
                    {
                        string wrapped = "{\"items\":" + raw + "}";
                        var arr = JsonUtility.FromJson<VocaDTOArray>(wrapped);
                        if (arr?.items != null && arr.items.Length > 0) dto = arr.items[0];
                    }
                    else
                    {
                        Debug.LogError("JSON 형식이 아님(HTML/문자열).");
                        continue;
                    }
                }

                if (dto != null && !string.IsNullOrEmpty(dto.vocabulary))
                    maker.MakeFallingWord(dto.vocabulary); // X 좌표 랜덤
            }

            // (선택) 너무 동시에 나오면 약간 지연
            // yield return new WaitForSeconds(0.15f);
        }
    }
}

