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
            req.SetRequestHeader("Accept", "application/json"); // �� JSON ��û
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

            // �� JSON�� �ƴϸ� (����ó�� text/html) �Ľ� ����
            if (!ct.ToLower().Contains("application/json"))
            {
                Debug.LogError("JSON�� �ƴ϶� HTML�� �Խ��ϴ�. �������� /api/voca/one�� JSON�� ��ȯ�ϵ��� �����ϼ���.");
                yield break;
            }

            try
            {
                var dto = JsonUtility.FromJson<VocaDTO>(raw);
                Debug.Log($"�ܾ�: {dto.vocabulary}");
                Debug.Log($"��: {dto.description}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"JSON �Ľ� ����: {ex.Message}");
            }
        }
    }
}
