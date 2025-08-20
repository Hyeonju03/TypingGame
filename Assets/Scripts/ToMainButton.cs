using UnityEngine;
using UnityEngine.UI;

public class ToMainButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            // OnClick �̺�Ʈ�� GoToMainScene �Լ��� ���� ����
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        Debug.Log("ToMainButton Clicked!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainScene();
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }
}