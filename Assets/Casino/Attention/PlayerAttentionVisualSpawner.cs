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

        [Tooltip("Где должен появиться объект (например, кость руки или головы в риге персонажа).")]
        [SerializeField] private Transform spawnPoint;

        private PlayerAttentionController _attentionController;
        private GameObject _spawnedInstance;

        private void Awake()
        {
            _attentionController = GetComponent<PlayerAttentionController>();
        }

        private void Start()
        {
            // Оптимизация: создаем объект один раз при загрузке персонажа и сразу выключаем.
            // Это избавляет от просадок кадров из-за Instantiate/Destroy при быстром переключении бинокля.
            if (visualPrefab != null && spawnPoint != null && _spawnedInstance == null)
            {
                _spawnedInstance = Instantiate(visualPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                _spawnedInstance.SetActive(false);
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
            if (_spawnedInstance != null)
            {
                _spawnedInstance.SetActive(isAttentionActive);
            }
        }
    }
}