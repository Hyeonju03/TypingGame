using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class VocaDTO { public string vocabulary; public string description; }

public class VocaShowTest : MonoBehaviour
{
    private const string Url = "http://localhost:9001/api/voca/one";

    private IEnumerator Start()
    {
        using (var req = UnityWebRequest.Get(Url))
        {
            req.SetRequestHeader("Accept", "application/json"); // ★ JSON 요청
            yield return req.SendWebRequest();

            var ct = req.GetResponseHeader("Content-Type") ?? "";
            var raw = req.downloadHandler.text;

            Debug.Log($"HTTP {req.responseCode}");
            Debug.Log($"Content-Type: {ct}");
            Debug.Log($"RAW: {raw}");

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                yield break;
            }

            // ★ JSON이 아니면 (지금처럼 text/html) 파싱 금지
            if (!ct.ToLower().Contains("application/json"))
            {
                Debug.LogError("JSON이 아니라 HTML이 왔습니다. 서버에서 /api/voca/one이 JSON을 반환하도록 설정하세요.");
                yield break;
            }

            try
            {
                var dto = JsonUtility.FromJson<VocaDTO>(raw);
                Debug.Log($"단어: {dto.vocabulary}");
                Debug.Log($"뜻: {dto.description}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"JSON 파싱 실패: {ex.Message}");
            }
        }
    }
}
