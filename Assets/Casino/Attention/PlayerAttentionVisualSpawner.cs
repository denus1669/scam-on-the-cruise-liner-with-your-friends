using System.Globalization;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Управляет визуальным объектом (например, моделью бинокля в руках).
    /// Включает и выключает объект вместо его постоянного создания/удаления.
    /// Объект отображается ТОЛЬКО для других игроков (сторонних наблюдателей).
    /// </summary>
    [RequireComponent(typeof(PlayerAttentionController))]
    public class PlayerAttentionVisualSpawner : MonoBehaviour
    {
        [Header("Настройки визуала")]
        [Tooltip("Префаб визуального объекта (без NetworkObject!).")]
        [SerializeField] private GameObject visualPrefab;
        private MeshRenderer meshRenderer;

        private PlayerAttentionController _attentionController;

        private void Awake()
        {
            _attentionController = GetComponent<PlayerAttentionController>();

            if (visualPrefab != null)
            {
                meshRenderer = visualPrefab.GetComponent<MeshRenderer>();
                meshRenderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            // Подписываемся на ГЛОБАЛЬНОЕ событие (оно синхронно сработает на всех клиентах через NetworkVariable)
            _attentionController.OnGlobalAttentionChanged += HandleGlobalAttentionChanged;
        }

        private void OnDisable()
        {
            _attentionController.OnGlobalAttentionChanged -= HandleGlobalAttentionChanged;
        }

        private void HandleGlobalAttentionChanged(bool isAttentionActive)
        {
            // Если этот скрипт выполняется на НАШЕМ локальном персонаже,
            // мы выходим, чтобы модель бинокля не заслоняла обзор и камеру игрока.
            if (_attentionController.IsOwner) return;

            // Для всех остальных наблюдателей по сети — включаем или выключаем модель бинокля
            if (meshRenderer != null)
            {
                meshRenderer.enabled = isAttentionActive;
            }
        }
    }
}