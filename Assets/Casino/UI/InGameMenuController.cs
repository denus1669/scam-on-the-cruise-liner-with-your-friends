using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

public class InGameMenuController : MonoBehaviour
{
    [Header("События")]
    [SerializeField] private GameEvent onMenuPressed;

    [Header("Переключатель управления")]
    [SerializeField] private InputModeSwitcher inputModeSwitcher;

    [Header("Панели")]
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private GameObject settingsAudioPanel;
    [SerializeField] private SettingsInputPanel settingsInputPanel;
    [SerializeField] private GameObject confirmQuitPanel;

    [Header("Кнопка назад в SettingsAudio")]
    [SerializeField] private UnityEngine.UI.Button settingsAudioBackButton;

    private NetworkObject _networkObject;


    private bool _isMenuOpen;
    private void Awake()
    {
        _networkObject = GetComponentInParent<NetworkObject>();
        if (_networkObject == null)
        {
            Debug.LogWarning("[InGameMenuController] NetworkObject not found! Menu will work for all players.", this);
        }
    }

    private void OnEnable()
    {
        onMenuPressed?.RegisterListener(ToggleMenu);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.OnContinuePressed += CloseMenu;
            mainMenuPanel.OnSettingsPressed += OpenSettings;
            mainMenuPanel.OnQuitPressed += ShowQuitConfirm;
        }

        if (settingsPanel != null)
        {
            settingsPanel.OnAudioPressed += OpenSettingsAudio;
            settingsPanel.OnInputPressed += OpenSettingsInput;
            settingsPanel.OnBackPressed += CloseSettings;
        }

        if (settingsInputPanel != null)
        {
            settingsInputPanel.OnBackPressed += CloseSettingsInput;
        }

        if (settingsAudioBackButton != null)
        {
            settingsAudioBackButton.onClick.AddListener(CloseSettingsAudio);
        }
    }

    private void OnDisable()
    {
        onMenuPressed?.UnregisterListener(ToggleMenu);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.OnContinuePressed -= CloseMenu;
            mainMenuPanel.OnSettingsPressed -= OpenSettings;
            mainMenuPanel.OnQuitPressed -= ShowQuitConfirm;
        }

        if (settingsPanel != null)
        {
            settingsPanel.OnAudioPressed -= OpenSettingsAudio;
            settingsPanel.OnInputPressed -= OpenSettingsInput;
            settingsPanel.OnBackPressed -= CloseSettings;
        }

        if (settingsInputPanel != null)
        {
            settingsInputPanel.OnBackPressed -= CloseSettingsInput;
        }

        if (settingsAudioBackButton != null)
        {
            settingsAudioBackButton.onClick.RemoveListener(CloseSettingsAudio);
        }
    }

    private void Start()
    {
        HideAllPanels();
    }

    private void ToggleMenu()
    {
        if (_networkObject != null && !_networkObject.IsOwner)
        {
            return;
        }

        if (_isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        _isMenuOpen = true;
        inputModeSwitcher?.EnterUIMode(true);

        HideAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        _isMenuOpen = false;
        HideAllPanels();
        inputModeSwitcher?.ExitUIMode(true);


        StartCoroutine(ForceCursorLockNextFrame());

    }

    private System.Collections.IEnumerator ForceCursorLockNextFrame()
    {
        yield return null; // Ждем следующий кадр
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #region Navigation

    private void OpenSettings()
    {
        HideAllPanels();
        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(true);
    }

    private void CloseSettings()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
            mainMenuPanel.gameObject.SetActive(true);
    }

    private void OpenSettingsAudio()
    {
        HideAllPanels();
        if (settingsAudioPanel != null)
            settingsAudioPanel.SetActive(true);
    }

    private void CloseSettingsAudio()
    {
        HideAllPanels();
        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(true);
    }

    private void OpenSettingsInput()
    {
        HideAllPanels();
        if (settingsInputPanel != null)
            settingsInputPanel.gameObject.SetActive(true);
    }

    private void CloseSettingsInput()
    {
        HideAllPanels();
        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(true);
    }

    private void ShowQuitConfirm()
    {
        HideAllPanels();
        if (confirmQuitPanel != null)
            confirmQuitPanel.SetActive(true);
    }

    public void CancelQuit()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
            mainMenuPanel.gameObject.SetActive(true);
    }

    #endregion

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
        if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
        if (settingsAudioPanel != null) settingsAudioPanel.SetActive(false);
        if (settingsInputPanel != null) settingsInputPanel.gameObject.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
    }
}