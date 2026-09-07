using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Клиентский компонент визуальных эффектов взрыва автомата.
    /// Подписывается на событие OnSlotMachineExploded от ядра и запускает партиклы, звук и тряску камеры.
    /// Работает только на клиентах (не на сервере).
    /// </summary>
    public class SlotMachineExplosionVFX : NetworkBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Ядро автомата, на событие которого мы подписываемся")]
        [SerializeField] private SlotMachine slotMachine;

        [Header("Партиклы")]
        [Tooltip("Система частиц взрыва (искры, дым, огонь)")]
        [SerializeField] private ParticleSystem explosionParticles;

        [Tooltip("Дополнительный эффект дыма после взрыва (опционально)")]
        [SerializeField] private ParticleSystem smokeParticles;

        [SerializeField] private ParticleSystem winParticles;

        [Header("Звук")]
        [Tooltip("Источник звука для воспроизведения взрыва")]
        [SerializeField] private AudioSource explosionAudioSource;

        [Tooltip("Звук взрыва")]
        [SerializeField] private AudioClip explosionSound;

        [Header("Тряска камеры")]
        [Tooltip("Включить тряску камеры при взрыве")]
        [SerializeField] private bool enableCameraShake = true;

        [Tooltip("Интенсивность тряски камеры")]
        [SerializeField] private float shakeIntensity = 0.5f;

        [Tooltip("Длительность тряски камеры в секундах")]
        [SerializeField] private float shakeDuration = 0.3f;

        [Header("Визуальные эффекты")]
        [Tooltip("Скрыть модель автомата после взрыва")]
        [SerializeField] private bool hideModelAfterExplosion = true;

        [Tooltip("Объект с мешем автомата (для скрытия)")]
        [SerializeField] private GameObject machineModel;

        private void Awake()
        {
            if (slotMachine == null)
            {
                slotMachine = GetComponent<SlotMachine>();
                if (slotMachine == null)
                {
                    Debug.LogError($"[SlotMachineExplosionVFX] SlotMachine не найден на {gameObject.name}");
                    return;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // VFX работают только на клиентах
            if (!IsServer)
            {
                slotMachine.OnSlotMachineExplosionChanged += HandleExplosionStateChanged;

                // Проверяем текущее состояние при спавне
                if (slotMachine.IsExploded)
                {
                    // Автомат уже взорван при спавне
                    if (hideModelAfterExplosion && machineModel != null)
                    {
                        machineModel.SetActive(false);
                    }
                }
            }
        }
        public override void OnNetworkDespawn()
        {
            if (!IsServer && slotMachine != null)
            {
                slotMachine.OnSlotMachineExplosionChanged -= HandleExplosionStateChanged;
            }

            base.OnNetworkDespawn();
        }

        private void HandleExplosionStateChanged(bool isExploded)
        {
            if (isExploded)
                HandleExplosion();
            else
                HandleRestored();
        }

        public void TriggerWinParticles()
        {
            StartCoroutine(PlayWinParticlesTimed());
        }

        private IEnumerator PlayWinParticlesTimed()
        {
            if (winParticles == null) yield break;

            winParticles.Play();

            // Ждём несколько секунд (например, 3)
            yield return new WaitForSeconds(3f);

            winParticles.Stop();
            winParticles.Clear(); // Очищаем оставшиеся частицы
        }

        /// <summary>
        /// Обработчик события взрыва от ядра автомата.
        /// Запускает все визуальные и звуковые эффекты.
        /// </summary>
        private void HandleExplosion()
        {
            Debug.Log($"[VFX] Запуск эффектов взрыва для автомата '{slotMachine.slotMachineName}'");

            // Запускаем партиклы взрыва
            PlayExplosionParticles();

            // Воспроизводим звук взрыва
            PlayExplosionSound();

            // Тряска камеры (только для локального игрока)
            if (enableCameraShake && IsLocalPlayer)
            {
                TriggerCameraShake();
            }

            // Скрываем модель автомата после взрыва
            if (hideModelAfterExplosion && machineModel != null)
            {
                machineModel.SetActive(false);
            }
        }

        private void PlayExplosionParticles()
        {
            if (explosionParticles != null)
            {
                explosionParticles.Play();
            }

            if (smokeParticles != null)
            {
                smokeParticles.Play();
            }
        }

        private void PlayExplosionSound()
        {
            if (explosionAudioSource != null && explosionSound != null)
            {
                explosionAudioSource.PlayOneShot(explosionSound);
            }
        }

        private void PlayWinParticles()
        {
            if (winParticles != null)
            {
                winParticles.Play();
            }
        }

        /// <summary>
        /// Запускает тряску камеры через Coroutine.
        /// Это простая реализация - для продакшена лучше использовать специализированную систему тряски.
        /// </summary>
        private void TriggerCameraShake()
        {
            StartCoroutine(CameraShakeCoroutine());
        }

        private System.Collections.IEnumerator CameraShakeCoroutine()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) yield break;

            Vector3 originalPosition = mainCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeIntensity;
                float y = Random.Range(-1f, 1f) * shakeIntensity;

                mainCamera.transform.position = originalPosition + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Возвращаем камеру в исходное положение
            mainCamera.transform.position = originalPosition;
        }

        /// <summary>
        /// Обработчик события восстановления автомата.
        /// Восстанавливает видимость модели автомата.
        /// </summary>
        private void HandleRestored()
        {
            Debug.Log($"[VFX] Восстановление автомата '{slotMachine.slotMachineName}'");

            // Восстанавливаем модель автомата
            if (hideModelAfterExplosion && machineModel != null)
            {
                machineModel.SetActive(true);
            }

            // Останавливаем эффекты дыма, если они ещё работают
            if (smokeParticles != null && smokeParticles.isPlaying)
            {
                smokeParticles.Stop();
            }
        }
    }
}