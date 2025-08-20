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
            // OnClick 이벤트에 GoToMainScene 함수를 직접 연결
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