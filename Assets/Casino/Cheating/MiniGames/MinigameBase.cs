using System;
using UnityEngine;

public abstract class MinigameBase : MonoBehaviour
{
    protected Action<bool> _onComplete;
    protected string _contextData;
    protected bool _isPlaying = false;

    [Header("Базовые настройки")]
    [SerializeField] protected GameObject panelRoot;

    [Header("Настройки курсора")]
    [Tooltip("Показывать курсор и разблокировать его при старте")]
    [SerializeField] protected bool manageCursor = true;

    // Приватные ссылки, найденные автоматически
    private MonoBehaviour _playerInputComponent;
    private MonoBehaviour _playerMovementComponent;
    private MonoBehaviour _playerCameraComponent;

    private bool _wasInputEnabled;
    private bool _wasMovementEnabled;
    private bool _wasCameraLookEnabled;
    private bool _componentsFound = false;

    public virtual void StartGame(string contextData, Action<bool> onComplete)
    {
        _contextData = contextData;
        _onComplete = onComplete;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (manageCursor)
            SetCursorState(true);

        FindAndBlockPlayerControl();
        OnGameStarted();
    }

    protected virtual void OnGameStarted() { }

    protected void FinishGame(bool isSuccess)
    {
        if (manageCursor)
            SetCursorState(false);
        RestorePlayerControl();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        _onComplete?.Invoke(isSuccess);
        _onComplete = null;
    }

    public virtual void ForceClose()
    {
        if (manageCursor)
            SetCursorState(false);
        RestorePlayerControl();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        _onComplete = null;
        _isPlaying = false;
    }

    public virtual void WinGame()
    {
        Debug.Log("<color=green>Выигрыш</color>");
        FinishGame(true);
    }

    public virtual void LoseGame()
    {
        Debug.Log("<color=red>Проигрыш</color>");
        FinishGame(false);
    }

    #region Auto Control Blocking

    private void FindAndBlockPlayerControl()
    {
        if (!_componentsFound || _playerInputComponent == null)
        {
            var manager = GetComponentInParent<PlayerMinigameManager>();
            if (manager == null) manager = GetComponent<PlayerMinigameManager>();

            if (manager != null)
            {
                var allComponents = manager.GetComponents<MonoBehaviour>();
                foreach (var comp in allComponents)
                {
                    var typeName = comp.GetType().Name;

                    // 1. Блокировка основного ввода (прыжки, ходьба)
                    if (_playerInputComponent == null &&
                        (typeName.Contains("PlayerInput") || typeName.Contains("CoreInput")))
                        _playerInputComponent = comp;

                    // 2. Блокировка движения
                    if (_playerMovementComponent == null &&
                        (typeName.Contains("Movement") || typeName.Contains("CoreMovement")))
                        _playerMovementComponent = comp;

                    // 3. Блокировка вращения камеры (Cinemachine input provider / CoreCamera)
                    if (_playerCameraComponent == null &&
                        (typeName.Contains("CoreCamera") || typeName.Contains("PlayerAttentionCamera") ||
                         typeName.Contains("CinemachineInputProvider")))
                        _playerCameraComponent = comp;
                }
                _componentsFound = true;
            }
            else
            {
                Debug.LogWarning("[MinigameBase] PlayerMinigameManager не найден для автоблокировки.");
            }
        }

        // Отключаем ввод
        ToggleComponent(_playerInputComponent, false, ref _wasInputEnabled);

        // Отключаем движение
        ToggleComponent(_playerMovementComponent, false, ref _wasMovementEnabled);

        // Отключаем вращение камеры
        // Для CoreCamera пытаемся установить enableLookInput = false через рефлексию,
        // иначе просто отключаем компонент целиком
        if (_playerCameraComponent != null)
        {
            var lookProp = _playerCameraComponent.GetType().GetProperty("enableLookInput");
            if (lookProp != null && lookProp.PropertyType == typeof(bool))
            {
                _wasCameraLookEnabled = (bool)lookProp.GetValue(_playerCameraComponent);
                lookProp.SetValue(_playerCameraComponent, false);
            }
            else
            {
                // Fallback: отключаем весь компонент камеры
                ToggleComponent(_playerCameraComponent, false, ref _wasCameraLookEnabled);
            }
        }
    }

    private void RestorePlayerControl()
    {
        ToggleComponent(_playerInputComponent, _wasInputEnabled, ref _wasInputEnabled);
        ToggleComponent(_playerMovementComponent, _wasMovementEnabled, ref _wasMovementEnabled);

        if (_playerCameraComponent != null)
        {
            var lookProp = _playerCameraComponent.GetType().GetProperty("enableLookInput");
            if (lookProp != null && lookProp.PropertyType == typeof(bool))
            {
                if (_wasCameraLookEnabled)
                    lookProp.SetValue(_playerCameraComponent, true);
            }
            else
            {
                ToggleComponent(_playerCameraComponent, _wasCameraLookEnabled, ref _wasCameraLookEnabled);
            }
        }
    }

    private void ToggleComponent(MonoBehaviour comp, bool enable, ref bool wasEnabled)
    {
        if (comp == null) return;
        if (enable && !comp.enabled) comp.enabled = true;
        else if (!enable && comp.enabled)
        {
            wasEnabled = true;
            comp.enabled = false;
        }
        else wasEnabled = false;
    }

    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    #endregion
}