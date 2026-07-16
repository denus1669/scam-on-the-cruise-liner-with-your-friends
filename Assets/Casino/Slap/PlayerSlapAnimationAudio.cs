using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Компонент для визуализации шлепков (анимации, воспроизведение звуков).
/// Слушает события из PlayerSlapReceiver и адаптирует громкость/типы звуков под силу удара.
/// </summary>
public class PlayerSlapAnimationAudio : MonoBehaviour
{
    [Header("Компоненты отображения")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Аудио Клипы")]
    [SerializeField] private AudioClip playerRecieveSlapSound; // Обычный шлепок
    [SerializeField] private AudioClip playerHeavySlapSound;   // Сильный удар (с отбрасыванием)
    [SerializeField] private AudioClip playerAttackSlapSound;  // Звук замаха/атаки

    [Header("Триггеры для Игрока")]
    [SerializeField] private readonly int playerRecieveSlapTrigger = Animator.StringToHash("Player_Slap_Recieved"); // Жертва получила шлепок
    [SerializeField] private readonly int playerAttackSlapTrigger = Animator.StringToHash("Player_Slap_Attack");    // Бьющий наносит удар

    [Header("Ссылка на ядро")]
    [SerializeField] private PlayerSlapReceiver playerSlapReceiver;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (playerSlapReceiver == null) playerSlapReceiver = GetComponent<PlayerSlapReceiver>();
    }

    private void OnEnable()
    {
        if (playerSlapReceiver != null)
        {
            // Подписываемся на события получения и совершения удара
            playerSlapReceiver.OnSlapReceived += PlayPlayerSlapReceivedVisuals;
            //playerSlapReceiver.OnSlapAttacked += PlayPlayerSlapAttackedVisuals;
        }
    }

    private void OnDisable()
    {
        if (playerSlapReceiver != null)
        {
            playerSlapReceiver.OnSlapReceived -= PlayPlayerSlapReceivedVisuals;
            //playerSlapReceiver.OnSlapAttacked -= PlayPlayerSlapAttackedVisuals;
        }
    }

    /// <summary>
    /// Отыгрывает визуал и звук ПОЛУЧЕНИЯ удара жертвой.
    /// </summary>
    /// <param name="interactorClientId">Кто ударил.</param>
    /// <param name="forceNormalized">Сила удара от 0 до 1.</param>
    private void PlayPlayerSlapReceivedVisuals(ulong interactorClientId, float forceNormalized)
    {
        Debug.Log($"[SlapVisuals] Игрок {gameObject.name} получил шлепок силой {forceNormalized:F2}");

        if (animator != null)
        {
            animator.SetTrigger(playerRecieveSlapTrigger);
        }

        // Динамический выбор звука и громкости на основе силы заряда
        if (forceNormalized > 0.5f && playerHeavySlapSound != null)
        {
            PlaySound(playerHeavySlapSound, forceNormalized);
        }
        else
        {
            PlaySound(playerRecieveSlapSound, Mathf.Max(0.3f, forceNormalized));
        }
    }

    /// <summary>
    /// Отыгрывает визуал и звук АТАКИ бьющего игрока.
    /// </summary>
    /// <param name="victimClientId">Кому наносится удар.</param>
    private void PlayPlayerSlapAttackedVisuals(ulong victimClientId)
    {
        Debug.Log($"[SlapVisuals] Игрок {gameObject.name} нанес удар игроку {victimClientId}");

        if (animator != null)
        {
            animator.SetTrigger(playerAttackSlapTrigger);
        }
        PlaySound(playerAttackSlapSound, 1f);
    }

    private void PlaySound(AudioClip clip, float volumeScale)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volumeScale);
        }
    }
}