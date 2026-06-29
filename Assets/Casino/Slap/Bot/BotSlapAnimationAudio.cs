using UnityEngine;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Универсальный визуальный контроллер для механики Slap.
    /// Не зависит от сети (NGO), так как слушает локальные C# события, 
    /// которые вызываются сетевыми компонентами (через ClientRpc).
    /// </summary>
    /// 

    public class BotSlapAnimationAudio : MonoBehaviour
{
    [Header("Компоненты отображения")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Аудио Клипы")]
    [SerializeField] private AudioClip botCheatsSlapSound;        
    [SerializeField] private AudioClip botBluffSlapSound;      
    [SerializeField] private AudioClip botIdleSlapSound;     


    [Header("Триггеры для бота")]
    [SerializeField] private readonly int botCheatingTrigger = Animator.StringToHash("Bot_Slap_Cheat_Recieved");      // Поймали на мухлеже
    [SerializeField] private readonly int botBluffTrigger = Animator.StringToHash("Bot_Slap_Bluff_Recieved");      // Игрок повелся на блеф
    [SerializeField] private readonly int botIdleSlapTrigger = Animator.StringToHash("Bot_Slap_Idle_Recieved");     // Бот злится, так как его шлепнули просто так

        // Ссылки для динамической подписки на этом объекте
    [SerializeField] private BotSlapRouter _botSlapRouter;         // Если это бот (получатель)

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Пытаемся найти компоненты на этом же объекте
        if (_botSlapRouter == null) _botSlapRouter = GetComponent<BotSlapRouter>();
    }

    private void OnEnable()
    {

        // Если на объекте есть маршрутизатор шлепков для Бота
        if (_botSlapRouter != null)
        {
            _botSlapRouter.OnCheatSlapped += PlayBotCheatSlappedVisuals;
            _botSlapRouter.OnBluffSlapped += PlayBotBluffSlappedVisuals;
            _botSlapRouter.OnIdleSlapped += PlayBotIdleSlappedVisuals;
        }

        Debug.Log($"[CharacterSlapVisuals]  {_botSlapRouter != null} .");
        }

    private void OnDisable()
    {

        if (_botSlapRouter != null)
        {
            _botSlapRouter.OnCheatSlapped -= PlayBotCheatSlappedVisuals;
            _botSlapRouter.OnBluffSlapped -= PlayBotBluffSlappedVisuals;
            _botSlapRouter.OnIdleSlapped -= PlayBotIdleSlappedVisuals;
        }
    }

    #region Эффекты Жертвы-Бота (Реакции на стратегии)

    private void PlayBotCheatSlappedVisuals()
    {
        // Бота поймали!
        if (animator != null) animator.SetTrigger(botCheatingTrigger);
        PlaySound(botCheatsSlapSound);
    }

    private void PlayBotBluffSlappedVisuals()
    {
        // Игрок купился на блеф
        if (animator != null) animator.SetTrigger(botBluffTrigger);
        PlaySound(botBluffSlapSound);
    }

    private void PlayBotIdleSlappedVisuals()
    {
        // Бота ударили без причины
        if (animator != null) animator.SetTrigger(botIdleSlapTrigger);
        PlaySound(botIdleSlapSound);
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
}
