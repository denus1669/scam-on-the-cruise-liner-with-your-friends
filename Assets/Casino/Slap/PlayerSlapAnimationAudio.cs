using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

public class PlayerSlapAnimationAudio : MonoBehaviour
{
    [Header("Компоненты отображения")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Аудио Клипы")]
    [SerializeField] private AudioClip playerRecieveSlapSound;
    [SerializeField] private AudioClip playerAttackSlapSound;

    [Header("Триггеры для Игрока")]
    [SerializeField] private readonly int playerRecieveSlapTrigger = Animator.StringToHash("Player_Slap_Recieved");      // Игрок получил шлепок
    [SerializeField] private readonly int playerAttackSlapTrigger = Animator.StringToHash("Player_Slap_Attack");      // Игрок наносит шлепок
    
    [Header("Ссылка для Игрока")]
    [SerializeField] private PlayerSlapReceiver playerSlapReceiver;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Пытаемся найти компоненты на этом же объекте
        if (playerSlapReceiver == null) playerSlapReceiver = GetComponent<PlayerSlapReceiver>();
    }

    private void OnEnable()
    {

        // Если на объекте есть маршрутизатор шлепков для Бота
        if (playerSlapReceiver != null)
        {
            playerSlapReceiver.OnSlapReceived += PlayPlayerSlapReceivedVisuals;
            //playerSlapReceiver.OnSlapAttacked += PlayPlayerSlapAttackedVisuals;
        }

        Debug.Log($"[CharacterSlapVisuals]  {playerSlapReceiver != null} .");
    }

    private void OnDisable()
    {

        if (playerSlapReceiver != null)
        {
            playerSlapReceiver.OnSlapReceived -= PlayPlayerSlapReceivedVisuals;
            //playerSlapReceiver.OnSlapAttacked -= PlayPlayerSlapAttackedVisuals;
        }
    }

    #region Эффекты Жертвы-Бота (Реакции на стратегии)
    private void PlayPlayerSlapReceivedVisuals(ulong interactorClientId)
    {
        Debug.Log($"[ANIM] Client-{NetworkManager.Singleton.LocalClientId}: " +
                      $"Playing slap animation on {gameObject.name}");

        if (animator != null)
        {
            Debug.Log($"[ANIM] Trigger set: {playerRecieveSlapTrigger}");
            animator.SetTrigger(playerRecieveSlapTrigger);
        }
        PlaySound(playerRecieveSlapSound);
    }

    private void PlayPlayerSlapAttackedVisuals(ulong interactorClientId)
    {
        // Игрок нанес шлепок
        if (animator != null) animator.SetTrigger(playerAttackSlapTrigger);
        PlaySound(playerAttackSlapSound);
    }

    #endregion

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

}
