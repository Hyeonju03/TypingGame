using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{
    public TMP_InputField inputField;

    // 다른 스크립트에서 접근할 수 있도록 public으로 선언
    public List<string> wordList = new List<string>();

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnSubmitInput);
        }
    }

    // 외부에서 단어를 추가할 때 호출할 메서드
    public void AddWord(string newWord)
    {
        wordList.Add(newWord);
        Debug.Log("새로운 단어가 추가되었습니다: " + newWord);
    }

    public void OnSubmitInput(string input)
    {

        string submittedText = input.Trim();
        bool found = false;

        for (int i = 0; i < wordList.Count; i++)
        {
            if (wordList[i] == submittedText)
            {
                Debug.Log("정답입니다! '" + submittedText + "'를 성공적으로 제거했습니다.");

                // 단어 리스트에서 제거
                wordList.RemoveAt(i);

                // (이 부분에 실제 게임 오브젝트를 찾아 제거하는 로직 추가)

                found = true;
                break;
            }
        }

        inputField.text = "";
        inputField.ActivateInputField();
        return;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
