using UnityEngine;
using UnityEngine.UI;

public class ConfirmQuitPanel : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private InGameMenuController menuController;

    private void OnEnable()
    {
        confirmButton?.onClick.AddListener(HandleConfirm);
        cancelButton?.onClick.AddListener(HandleCancel);
    }

    private void OnDisable()
    {
        confirmButton?.onClick.RemoveListener(HandleConfirm);
        cancelButton?.onClick.RemoveListener(HandleCancel);
    }

    private void HandleConfirm()
    {
        Debug.Log("[ConfirmQuit] Закрытие игры.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleCancel()
    {
        menuController?.CancelQuit();
    }
}