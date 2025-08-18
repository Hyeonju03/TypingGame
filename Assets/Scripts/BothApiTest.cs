// BothApiTest.cs — /api/learn/both 응답을 콘솔에 확실히 찍는 최소 테스트
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
/*public class BothDTO
{
    public string vocabulary;           // 단어
    public string descriptionWord;      // 단어 뜻
    public string munjang;              // 문장
    public string descriptionMunjang;   // 문장 뜻
}*/

public class BothApiTest : MonoBehaviour
{
    [SerializeField] string url = "http://localhost:9001/api/learn/both";

    void Start() => StartCoroutine(Fetch());

    private IEnumerator Fetch()
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            Debug.Log($"HTTP {req.responseCode} CT={req.GetResponseHeader("Content-Type")} URL={url}");

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("HTTP error: " + req.error);
                yield break;
            }

            var raw = req.downloadHandler.text ?? "";
            Debug.Log("RAW => " + raw);

            // 앞뒤 잡다한 문자 제거(BOM/제로폭/공백)
            raw = raw.TrimStart('\uFEFF', '\u200B', '\u0000').Trim();

            // 첫 유효 문자 검사
            int i = 0; while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
            if (i >= raw.Length || raw[i] != '{') { Debug.LogError("JSON 객체 아님"); yield break; }

            BothDTO dto;
            try
            {
                dto = JsonUtility.FromJson<BothDTO>(raw);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON parse 실패: " + ex.Message);
                yield break;
            }

            Debug.Log($"단어: {dto.vocabulary}");
            Debug.Log($"단어 뜻: {dto.descriptionWord}");
            Debug.Log($"문장: {dto.munjang}");
            Debug.Log($"문장 뜻: {dto.descriptionMunjang}");
        }
    }
}
