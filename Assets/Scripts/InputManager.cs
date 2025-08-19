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

    // ✅ 사용하지 않는 변수 제거
    // public int wordsToNextScene = 15;
    // private int wordsRemoved = 0;
    // public string nextSceneName;

    public event Action OnWordTyped;
    public Dictionary<string, List<GameObject>> wordObjectMap = new Dictionary<string, List<GameObject>>();

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnSubmitInput);
        }
        // TODO: 시작 시 페이드 인/아웃 로직
    }

    public void OnSubmitInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        string matchedWord = null;
        GameObject matchedObject = null;

        foreach (var pair in wordObjectMap)
        {
            if (pair.Key.Equals(input, StringComparison.OrdinalIgnoreCase))
            {
                matchedWord = pair.Key;
                matchedObject = pair.Value.FirstOrDefault(o => o != null);
                if (matchedObject != null)
                {
                    break;
                }
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

    // ... 기존 메서드들 (AddWordAndObject, RemoveWordAndObject, ClearAllWords 등)
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