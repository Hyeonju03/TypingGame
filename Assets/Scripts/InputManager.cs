using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
    [Header("Refs")]
    public TMP_InputField inputField;
    public CanvasGroup transitionCanvasGroup;

    [Header("Game State")]
    public int wordsToNextScene = 15;
    private int wordsRemoved = 0;

    [Header("Scene Management")]
    public string nextSceneName;

    public Dictionary<string, List<GameObject>> wordObjectMap = new Dictionary<string, List<GameObject>>();

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnSubmitInput);
        }

        StartCoroutine(Fade(transitionCanvasGroup, 1f, 0f, 0.5f));
    }

    public void AddWordAndObject(string newWord, GameObject obj)
    {
        if (!wordObjectMap.ContainsKey(newWord))
        {
            wordObjectMap[newWord] = new List<GameObject>();
        }
        wordObjectMap[newWord].Add(obj);
        Debug.Log($"[InputManager] 새로운 단어가 추가되었습니다: '{newWord}'. 현재 '{newWord}' 개수: {wordObjectMap[newWord].Count}");
    }

    public void OnSubmitInput(string input)
    {
        string submittedText = input.Trim();
        // ✅ 사용자가 입력한 값의 공백을 ^로 치환하여 비교
        string processedInput = submittedText.Replace(' ', '^');

        if (wordObjectMap.ContainsKey(processedInput))
        {
            var objects = wordObjectMap[processedInput];
            if (objects.Count > 0)
            {
                // ✅ 가장 아래쪽에 있는 오브젝트를 제거 (먼저 도달한 단어)
                GameObject targetObj = objects.OrderBy(o => o.transform.position.y).FirstOrDefault();

                if (targetObj != null)
                {
                    Debug.Log("정답입니다! '" + submittedText + "'를 성공적으로 제거했습니다.");

                    objects.Remove(targetObj);
                    Destroy(targetObj);

                    if (objects.Count == 0)
                    {
                        wordObjectMap.Remove(processedInput);
                    }

                    wordsRemoved++;
                    Debug.Log($"[InputManager] 제거된 단어 수: {wordsRemoved}");

                    if (wordsRemoved >= wordsToNextScene)
                    {
                        StartCoroutine(FadeAndLoadScene(transitionCanvasGroup, 0f, 1f, 0.5f, nextSceneName));
                        return;
                    }
                }
            }
        }

        inputField.text = "";
        inputField.ActivateInputField();
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
            Debug.Log($"[InputManager] 딕셔너리에서 '{wordToRemove}' 오브젝트 제거됨.");

            if (objects.Count == 0)
            {
                wordObjectMap.Remove(wordToRemove);
            }
        }
    }

    private IEnumerator Fade(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }

    private IEnumerator FadeAndLoadScene(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration, string sceneName)
    {
        yield return StartCoroutine(Fade(canvasGroup, startAlpha, endAlpha, duration));
        SceneManager.LoadScene(sceneName);
    }
}