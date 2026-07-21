using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Компонент для визуализации и звуков слот-машины.
    /// Слушает события из SlotMachine и отыгрывает анимации/звуки.
    /// Работает только на клиентах (не на сервере).
    /// </summary>
    public class SlotMachineAnimationAudio : MonoBehaviour
    {
        [Header("Компоненты отображения")]
        [SerializeField] private Animator wheelAnimator;
        [SerializeField] private AudioSource audioSource;

        [Header("Аудио Клипы")]
        [SerializeField] private AudioClip spinSound;        // Звук вращения барабанов
        [SerializeField] private AudioClip winSound;         // Звук победы/джекпота
        [SerializeField] private AudioClip loseSound;        // Звук проигрыша
        [SerializeField] private AudioClip breakdownSound;   // Звук поломки
        [SerializeField] private AudioClip explosionSound;   // Звук взрыва
        [SerializeField] private AudioClip fixSound;         // Звук починки

        [Header("Триггеры для Аниматора")]
        [SerializeField] private readonly int spinTrigger = Animator.StringToHash("Spin");
        [SerializeField] private readonly int winTrigger = Animator.StringToHash("Win");
        [SerializeField] private readonly int loseTrigger = Animator.StringToHash("Lose");
        [SerializeField] private readonly int breakdownTrigger = Animator.StringToHash("Breakdown");
        [SerializeField] private readonly int explosionTrigger = Animator.StringToHash("Explosion");

        [Header("Ссылка на ядро")]
        [SerializeField] private SlotMachine slotMachine;

        private void Awake()
        {
            if (wheelAnimator == null) wheelAnimator = GetComponentInChildren<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (slotMachine == null) slotMachine = GetComponentInParent<SlotMachine>();
        }

        private void OnEnable()
        {
            if (slotMachine != null)
            {
                // Подписываемся на все события слот-машины
                slotMachine.OnSpinStarted += PlaySpinVisuals;
                slotMachine.OnSpinCompleted += PlaySpinResultVisuals;
                slotMachine.OnSlotMachineBreakdownChanged += PlayBreakdownVisuals;
                slotMachine.OnSlotMachineExplosionChanged += PlayExplosionVisuals;
            }
        }

        private void OnDisable()
        {
            if (slotMachine != null)
            {
                slotMachine.OnSpinStarted -= PlaySpinVisuals;
                slotMachine.OnSpinCompleted -= PlaySpinResultVisuals;
                slotMachine.OnSlotMachineBreakdownChanged -= PlayBreakdownVisuals;
                slotMachine.OnSlotMachineExplosionChanged -= PlayExplosionVisuals;
            }
        }

        /// <summary>
        /// Отыгрывает визуал и звук начала спина.
        /// </summary>
        private void PlaySpinVisuals(float duration)
        {
            Debug.Log($"[SlotMachineVisuals] Спин начался, длительность: {duration}с");

            if (wheelAnimator != null)
            {
                wheelAnimator.SetFloat("SpinDuration", duration);
                wheelAnimator.SetTrigger(spinTrigger);
            }

            PlaySound(spinSound, 1f);
        }

        /// <summary>
        /// Отыгрывает визуал и звук результата спина (победа/проигрыш).
        /// </summary>
        private void PlaySpinResultVisuals(bool isWin, int comboIndex)
        {
            if (isWin)
            {
                Debug.Log($"[SlotMachineVisuals] ПОБЕДА! Комбинация #{comboIndex}");

                if (wheelAnimator != null)
                {
                    wheelAnimator.SetTrigger(winTrigger);
                }

                PlaySound(winSound, 1f);
            }
            else
            {
                Debug.Log($"[SlotMachineVisuals] Проигрыш");

                if (wheelAnimator != null)
                {
                    wheelAnimator.SetTrigger(loseTrigger);
                }

                PlaySound(loseSound, 0.7f);
            }
        }

        /// <summary>
        /// Отыгрывает визуал и звук поломки.
        /// </summary>
        private void PlayBreakdownVisuals(bool isBroken)
        {
            if (isBroken)
            {
                Debug.Log($"[SlotMachineVisuals] Автомат сломался!");

                if (wheelAnimator != null)
                {
                    wheelAnimator.SetTrigger(breakdownTrigger);
                }

                PlaySound(breakdownSound, 1f);
            }
        }

        /// <summary>
        /// Отыгрывает визуал и звук взрыва.
        /// </summary>
        private void PlayExplosionVisuals(bool isExploded)
        {
            if (isExploded)
            {
                Debug.Log($"[SlotMachineVisuals] Автомат взорвался!");

                if (wheelAnimator != null)
                {
                    wheelAnimator.SetTrigger(explosionTrigger);
                }

                PlaySound(explosionSound, 1f);
            }
        }

        private void PlaySound(AudioClip clip, float volumeScale)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volumeScale);
            }
        }
    }
}