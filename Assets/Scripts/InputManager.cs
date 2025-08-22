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
    public TMP_InputField inputField; // 화면 표시용(옵션)
    public event Action OnWordTyped;
    public Dictionary<string, List<GameObject>> wordObjectMap = new();

#if UNITY_WEBGL
    [DllImport("__Internal")] private static extern void InitExternalInput();
    [DllImport("__Internal")] private static extern void FocusExternalInput();
#endif

    // NEW: 같은 프레임 중복 제출 방지
    private int _lastSubmitFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; // Awake는 싱글턴만
    }

    private void Start()
    {
        if (inputField)
        {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(SubmitInputFromEditor);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
    WebGLInput.captureAllKeyboardInput = false;
    if (inputField) inputField.readOnly = true;
    InitExternalInput();   // 숨은 input 생성/바인딩
    FocusExternalInput();  // 초기 포커스
#endif
    }


    // JS → Unity: 현재 버퍼 동기화
    public void ReceiveInputFromWeb(string text)
    {
        if (!inputField) return;
        inputField.SetTextWithoutNotify(text);
        inputField.caretPosition = text.Length;
    }

    // JS/Editor 공용 제출
    public void SubmitInputFromWeb(string input)
    {
        // NEW: 같은 프레임에 두 번 이상 처리 금지
        if (_lastSubmitFrame == Time.frameCount) return;
        _lastSubmitFrame = Time.frameCount;

        if (inputField) StartCoroutine(ClearInputNextFrame());
        if (!string.IsNullOrWhiteSpace(input))
        {
            string normalized = NormalizeInput(input);
            foreach (var pair in wordObjectMap.ToList())
            {
                if (NormalizeInput(pair.Key).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    // CHANGED: 중복일 때 '가장 아래' 하나만 제거 (UI이면 anchoredPosition.y 기준)
                    var obj = pair.Value
                        .Where(o => o != null)
                        .OrderBy(o => (o.transform is RectTransform rt)
                                      ? rt.anchoredPosition.y
                                      : o.transform.position.y)
                        .FirstOrDefault();

                    if (obj != null)
                    {
                        RemoveWordAndObject(obj);
                        StartCoroutine(DestroyEndOfFrame(obj));
                        OnWordTyped?.Invoke();
                    }
                    break;
                }
            }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        FocusExternalInput(); // 다음 입력 대비
#endif
    }

    private System.Collections.IEnumerator ClearInputNextFrame()
    {
        yield return new WaitForEndOfFrame();
        if (!inputField) yield break;

        inputField.text = string.Empty;
        inputField.textComponent.text = string.Empty;
        inputField.textComponent.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

#if !UNITY_WEBGL || UNITY_EDITOR
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es) es.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
        inputField.caretPosition = 0;
#else
        FocusExternalInput();
#endif
    }

    private System.Collections.IEnumerator DestroyEndOfFrame(GameObject go)
    {
        if (go) go.SetActive(false);
        yield return new WaitForEndOfFrame();
        if (go) Destroy(go);
    }

    public void SubmitInputFromEditor(string input) => SubmitInputFromWeb(input);

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
            foreach (var obj in list)
                if (obj != null) Destroy(obj);
        wordObjectMap.Clear();
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private void Update()
    {
        if (inputField && inputField.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SubmitInputFromEditor(inputField.text);
        }
    }
#endif
}
