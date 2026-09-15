using System.Reflection;
using UnityEngine;

public interface IInputModeSwitcher
{
    void EnterUIMode(bool manageCursor);
    void ExitUIMode(bool manageCursor);
}

public class InputModeSwitcher : MonoBehaviour, IInputModeSwitcher
{
    private MonoBehaviour _movementComponent;
    private MonoBehaviour _cameraComponent;

    private bool _wasMovementEnabled;
    private bool _wasCameraLookEnabled;
    private bool _componentsFound;

    private bool _wasCursorVisible;
    private CursorLockMode _wasCursorLockState;
    private bool _cursorStateSaved;

    private void Awake()
    {
        FindPlayerComponents();
    }

    public void EnterUIMode(bool manageCursor)
    {
        if (manageCursor)
        {
            SaveCursorState();
            SetCursorVisible(true);
        }

        BlockPlayerControl();
    }

    public void ExitUIMode(bool manageCursor)
    {
        if (manageCursor)
            RestoreCursorState();

        RestorePlayerControl();
    }

    #region Component Search

    private void FindPlayerComponents()
    {
        if (_componentsFound) return;

        var allComponents = GetComponents<MonoBehaviour>();

        foreach (var comp in allComponents)
        {
            var typeName = comp.GetType().Name;

            // Движение (НЕ ввод!)
            if (_movementComponent == null &&
                (typeName.Contains("CoreMovement") || typeName.Contains("Movement")))
                _movementComponent = comp;

            // Камера
            if (_cameraComponent == null &&
                (typeName.Contains("CoreCamera") || typeName.Contains("PlayerAttentionCamera") ||
                 typeName.Contains("CinemachineInputProvider")))
                _cameraComponent = comp;
        }

        _componentsFound = true;
    }

    #endregion

    #region Block / Restore

    private void BlockPlayerControl()
    {
        // 1. Блокируем движение через публичное свойство
        SetMovementEnabled(false);

        // 2. Блокируем камеру через поле (не свойство!)
        SetCameraLookEnabled(false);
    }

    private void RestorePlayerControl()
    {
        SetMovementEnabled(true);
        SetCameraLookEnabled(true);
    }

    private void SetMovementEnabled(bool enable)
    {
        if (_movementComponent == null) return;

        var prop = _movementComponent.GetType().GetProperty("IsMovementEnabled",
            BindingFlags.Public | BindingFlags.Instance);

        if (prop != null && prop.PropertyType == typeof(bool))
        {
            if (!enable)
                _wasMovementEnabled = (bool)prop.GetValue(_movementComponent);

            // Восстанавливаем только если было включено до блокировки
            if (enable && !_wasMovementEnabled) return;

            prop.SetValue(_movementComponent, enable);
        }
    }

    private void SetCameraLookEnabled(bool enable)
    {
        if (_cameraComponent == null) return;

        var field = _cameraComponent.GetType().GetField("enableLookInput",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        if (field != null && field.FieldType == typeof(bool))
        {
            if (!enable)
                _wasCameraLookEnabled = (bool)field.GetValue(_cameraComponent);

            if (enable && !_wasCameraLookEnabled) return;

            field.SetValue(_cameraComponent, enable);
        }
    }

    #endregion

    #region Cursor

    private void SaveCursorState()
    {
        if (_cursorStateSaved) return;
        _wasCursorVisible = Cursor.visible;
        _wasCursorLockState = Cursor.lockState;
        _cursorStateSaved = true;
    }

    private void RestoreCursorState()
    {
        if (!_cursorStateSaved) return;
        Cursor.visible = _wasCursorVisible;
        Cursor.lockState = _wasCursorLockState;
        _cursorStateSaved = false;
        Debug.Log($"[InputModeSwitcher] RestoreCursorState: {Cursor.visible},    {Cursor.lockState}");
    }


    private void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;

    }

    #endregion
}