using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class VocaDTO { public string vocabulary; public string description; }

[System.Serializable]
class VocaDTOArray { public VocaDTO[] items; }

public class GameStartFromDB : MonoBehaviour
{
    [SerializeField] private FallingWordMaker maker; // Inspector�� ����
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

                // �� ����: ���� ���� + ���¿� ���� �Ľ�
                string raw = req.downloadHandler.text?.Trim();
                VocaDTO dto = null;

                if (!string.IsNullOrEmpty(raw))
                {
                    if (raw.StartsWith("{")) // ��ü
                    {
                        dto = JsonUtility.FromJson<VocaDTO>(raw);
                    }
                    else if (raw.StartsWith("[")) // �迭 �� ù ��°�� ���
                    {
                        string wrapped = "{\"items\":" + raw + "}";
                        var arr = JsonUtility.FromJson<VocaDTOArray>(wrapped);
                        if (arr?.items != null && arr.items.Length > 0) dto = arr.items[0];
                    }
                    else
                    {
                        Debug.LogError("JSON ������ �ƴ�(HTML/���ڿ�).");
                        continue;
                    }
                }

                if (dto != null && !string.IsNullOrEmpty(dto.vocabulary))
                    maker.MakeFallingWord(dto.vocabulary); // X ��ǥ ����
            }

            // (����) �ʹ� ���ÿ� ������ �ణ ����
            // yield return new WaitForSeconds(0.15f);
        }
    }
}

