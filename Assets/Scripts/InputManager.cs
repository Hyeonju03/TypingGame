using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System;

public class InputManager : MonoBehaviour
{
    [Header("Refs")]
    public TMP_InputField inputField;
    public CanvasGroup transitionCanvasGroup;

    public event Action OnWordTyped;
    public Dictionary<string, List<GameObject>> wordObjectMap = new Dictionary<string, List<GameObject>>();

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnSubmitInput);
        }
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

    // 공백과 ^를 모두 공백으로 치환하여 비교
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
