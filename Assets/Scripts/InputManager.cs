using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System.Runtime.InteropServices;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
 
    [Header("Refs")]
    public TMP_InputField inputField;
    public event Action OnWordTyped;
    public Dictionary<string, List<GameObject>> wordObjectMap = new Dictionary<string, List<GameObject>>();

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void FocusExternalInput();
#endif
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        if (inputField != null)
            inputField.readOnly = true;
    }

    private void Start()
    {
#if UNITY_WEBGL
        FocusExternalInput();
#endif
    }

    public void ReceiveInputFromWeb(string text)
    {
        if (inputField != null)
        {
            // 텍스트를 설정하되, 불필요한 이벤트 호출을 방지합니다.
            inputField.SetTextWithoutNotify(text);

            // 캐럿 위치를 가장 끝으로 이동시켜 커서가 올바르게 보이도록 합니다.
            inputField.caretPosition = text.Length;
        }
    }

    public void SubmitInputFromWeb(string input)
    {
        if (string.IsNullOrEmpty(input)) return;
        string normalizedInput = NormalizeInput(input);
        foreach (var pair in wordObjectMap)
        {
            if (NormalizeInput(pair.Key).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                var obj = pair.Value.FirstOrDefault();

                if (obj != null)
                {
                    Destroy(obj);
                    RemoveWordAndObject(obj);
                    OnWordTyped?.Invoke();
                    break;
                }
            }
        }
        inputField.text = "";

#if UNITY_WEBGL
        FocusExternalInput();
#endif
    }

    private string NormalizeInput(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace('^', ' ').Trim();

    public void AddWordAndObject(string word, GameObject obj)
    {
        if (!wordObjectMap.ContainsKey(word)) wordObjectMap[word] = new List<GameObject>();
        wordObjectMap[word].Add(obj);
    }

    public void RemoveWordAndObject(GameObject obj)
    {
        if (obj == null) return;
        string keyToRemove = wordObjectMap.FirstOrDefault(kv => kv.Value.Contains(obj)).Key;
        if (!string.IsNullOrEmpty(keyToRemove))
        {
            wordObjectMap[keyToRemove].Remove(obj);
            if (wordObjectMap[keyToRemove].Count == 0) wordObjectMap.Remove(keyToRemove);
        }
    }

    public void ClearAllWords()
    {
        foreach (var list in wordObjectMap.Values)
        {
            foreach (var obj in list)
            {
                if (obj != null) Destroy(obj);
            }
        }
        wordObjectMap.Clear();
    }

    public void OnLangKeyPressed(string dummy)
    {
        Debug.Log("한영키 감지됨 (Unity)");
    }

}