using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement; // 씬 전환을 위해 추가

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

    // 단어와 해당 게임 오브젝트를 매핑하여 관리
    public Dictionary<string, List<GameObject>> wordObjectMap = new Dictionary<string, List<GameObject>>();

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnSubmitInput);
        }

        StartCoroutine(Fade(transitionCanvasGroup, 1f, 0f, 0.5f));
    }

    // FallingWordMaker가 생성한 단어와 오브젝트를 전달받는 메서드
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

        if (wordObjectMap.ContainsKey(submittedText))
        {
            var objects = wordObjectMap[submittedText];
            if (objects.Count > 0)
            {
                GameObject targetObj = objects.OrderBy(o => o.transform.position.y).FirstOrDefault();

                if (targetObj != null)
                {
                    Debug.Log("정답입니다! '" + submittedText + "'를 성공적으로 제거했습니다.");

                    objects.Remove(targetObj);
                    Destroy(targetObj);

                    if (objects.Count == 0)
                    {
                        wordObjectMap.Remove(submittedText);
                    }

                    wordsRemoved++;
                    Debug.Log($"[InputManager] 제거된 단어 수: {wordsRemoved}");

                    if (wordsRemoved >= wordsToNextScene)
                    {
                        StartCoroutine(FadeAndLoadScene(transitionCanvasGroup, 0f, 1f, 0.5f, nextSceneName));
                        return; // 씬 로드 코루틴이 시작되면 더 이상 진행하지 않음
                    }
                }
            }
        }

        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void RemoveWordAndObject(string word, GameObject obj)
    {
        if (wordObjectMap.ContainsKey(word))
        {
            var objects = wordObjectMap[word];
            if (objects.Contains(obj))
            {
                objects.Remove(obj);
                Debug.Log($"[InputManager] 딕셔너리에서 '{word}' 오브젝트 제거됨.");

                if (objects.Count == 0)
                {
                    wordObjectMap.Remove(word);
                }
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
        // 페이드 아웃 효과 시작
        yield return StartCoroutine(Fade(canvasGroup, startAlpha, endAlpha, duration));

        // 씬 로드
        SceneManager.LoadScene(sceneName);
    }
}