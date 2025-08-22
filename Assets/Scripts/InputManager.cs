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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

#if UNITY_WEBGL && !UNITY_EDITOR
    if (inputField) inputField.readOnly = true;   // WebGL: 외부 입력만
    WebGLInput.captureAllKeyboardInput = false;   // 브라우저 입력 독점 해제
#else
        if (inputField) inputField.readOnly = false;  // 에디터/스탠드얼론: 직접 타이핑
#endif
    }


    private void Start()
    {
        if (inputField)
        {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(SubmitInputFromEditor); // Enter에 바로 제출
        }

#if UNITY_WEBGL && !UNITY_EDITOR
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
        Debug.Log($"[SubmitInputFromWeb] '{input}'");

        if (inputField) StartCoroutine(ClearInputNextFrame());
        if (string.IsNullOrWhiteSpace(input))
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        FocusExternalInput();
#endif
            return;
        }

        string normalizedInput = NormalizeInput(input);
        foreach (var pair in wordObjectMap.ToList())
        {
            if (NormalizeInput(pair.Key).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                var obj = pair.Value.FirstOrDefault();
                if (obj != null)
                {
                    RemoveWordAndObject(obj);              // 맵에서 먼저 제거
                    StartCoroutine(DestroyEndOfFrame(obj)); // 프레임 끝에서 안전 파괴
                    OnWordTyped?.Invoke();
                }
                break;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
    FocusExternalInput();
#endif
    }

    private System.Collections.IEnumerator ClearInputNextFrame()
    {
        yield return new WaitForEndOfFrame();   // 이벤트 정리 대기

        inputField.text = string.Empty;                 // 값 비우기
        inputField.textComponent.text = string.Empty;   // 라벨도 비우기
        inputField.textComponent.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

#if !UNITY_WEBGL || UNITY_EDITOR
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es) es.SetSelectedGameObject(inputField.gameObject); // 선택 재지정
        inputField.ActivateInputField();                         // 포커스 유지
        inputField.caretPosition = 0;
#else
    FocusExternalInput(); // WebGL은 숨은 input으로 포커스
#endif
    }

    private System.Collections.IEnumerator DestroyEndOfFrame(GameObject go)
    {
        if (go) go.SetActive(false);
        yield return new WaitForEndOfFrame();
        if (go) Destroy(go);
    }



    public void SubmitInputFromEditor(string input)
    {
        SubmitInputFromWeb(input); // 에디터에서도 동일 로직 재사용
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

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (inputField && inputField.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SubmitInputFromEditor(inputField.text); // 동일 로직으로 처리 + 비우기 코루틴 실행
        }
#endif
    }

}

