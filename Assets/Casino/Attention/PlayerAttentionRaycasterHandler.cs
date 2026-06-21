using Blocks.Gameplay.Core;
using UnityEngine;

/// <summary>
/// Отвечает ТОЛЬКО за включение/выключение луча сканера.
/// </summary>

[RequireComponent(typeof(PlayerAttentionController))]
public class PlayerAttentionRaycasterHandler : MonoBehaviour
{
    [SerializeField] private AttentionRaycaster attentionRaycaster;

    private PlayerAttentionController _playerAttentionController;

    private void Awake()
    {
        _playerAttentionController = GetComponent<PlayerAttentionController>();
    }

    private void OnEnable()
    {
        if (_playerAttentionController == null)
        {
            return;
        }
        // Подписываемся только на локальное событие (луч нужен только нам)
        _playerAttentionController.OnLocalAttentionChanged += ToggleRaycaster;
    }

    private void OnDisable()
    {
        if (_playerAttentionController == null)
        {
            return;
        }
        _playerAttentionController.OnLocalAttentionChanged -= ToggleRaycaster;
    }   
     
    private void ToggleRaycaster(bool isAttentionActive)
    {
        if (attentionRaycaster != null)
        {
            attentionRaycaster.SetRaycasterActive(isAttentionActive);
        }
    }
}
