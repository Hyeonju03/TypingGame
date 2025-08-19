using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance; // 👈 이 줄을 추가해야 합니다.

    [Header("Refs")]
    public TMP_InputField inputField;
    public CanvasGroup transitionCanvasGroup;

    public event Action OnWordTyped;
    public Dictionary<string, List<GameObject>> wordObjectMap = new Dictionary<string, List<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // InputManager는 씬마다 존재하므로 이 코드는 필요 없습니다.

        inputField = FindObjectOfType<TMP_InputField>();
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnSubmitInput);
        }
    }

    void Start()
    {
        // Start 함수에 기존에 있던 onEndEdit.AddListener 코드는 Awake로 옮겼으므로 여기서는 제거합니다.
        // 현재 코드에서는 Start에 아무것도 없습니다.
    }

    public void OnSubmitInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return;

        string matchedWord = null;
        GameObject matchedObject = null;

        string normalizedInput = NormalizeInput(input);

        foreach (var pair in wordObjectMap)
        {
            string normalizedKey = NormalizeInput(pair.Key);
            if (normalizedKey.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                matchedWord = pair.Key;
                matchedObject = pair.Value.FirstOrDefault(o => o != null);
                if (matchedObject != null) break;
            }
        }

        if (matchedObject != null)
        {
            Destroy(matchedObject);
            RemoveWordAndObject(matchedObject);
            OnWordTyped?.Invoke();
        }

        inputField.text = "";
        inputField.ActivateInputField();
    }

    private string NormalizeInput(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace('^', ' ').Trim();
    }

    public void AddWordAndObject(string newWord, GameObject obj)
    {
        if (!wordObjectMap.ContainsKey(newWord))
        {
            wordObjectMap[newWord] = new List<GameObject>();
        }
        wordObjectMap[newWord].Add(obj);
    }

    public void ClearAllWords()
    {
        foreach (var list in wordObjectMap.Values)
        {
            foreach (var obj in list)
            {
                Destroy(obj);
            }
        }
        wordObjectMap.Clear();
    }

    public void RemoveWordAndObject(GameObject obj)
    {
        if (obj == null) return;
        string wordToRemove = null;
        foreach (var entry in wordObjectMap)
        {
            if (entry.Value.Contains(obj))
            {
                wordToRemove = entry.Key;
                break;
            }
        }
        if (wordToRemove != null)
        {
            var objects = wordObjectMap[wordToRemove];
            objects.Remove(obj);
            if (objects.Count == 0)
            {
                wordObjectMap.Remove(wordToRemove);
            }
        }
    }
}