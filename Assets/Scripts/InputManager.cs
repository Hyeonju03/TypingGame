using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{
    public TMP_InputField inputField;

    // �ٸ� ��ũ��Ʈ���� ������ �� �ֵ��� public���� ����
    public List<string> wordList = new List<string>();

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnSubmitInput);
        }
    }

    // �ܺο��� �ܾ �߰��� �� ȣ���� �޼���
    public void AddWord(string newWord)
    {
        wordList.Add(newWord);
        Debug.Log("���ο� �ܾ �߰��Ǿ����ϴ�: " + newWord);
    }

    public void OnSubmitInput(string input)
    {

        string submittedText = input.Trim();
        bool found = false;

        for (int i = 0; i < wordList.Count; i++)
        {
            if (wordList[i] == submittedText)
            {
                Debug.Log("�����Դϴ�! '" + submittedText + "'�� ���������� �����߽��ϴ�.");

                // �ܾ� ����Ʈ���� ����
                wordList.RemoveAt(i);

                // (�� �κп� ���� ���� ������Ʈ�� ã�� �����ϴ� ���� �߰�)

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
