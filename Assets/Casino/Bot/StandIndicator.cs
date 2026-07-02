using UnityEngine;

public class StandIndicator : NetworkObjectVisibilityIndicator
{
    [SerializeField] private BaseBotBehavior botBehavior;

    protected override void Awake()
    {
        base.Awake();
        if (botBehavior == null) botBehavior = GetComponent<BaseBotBehavior>();
    }

    private void OnEnable()
    {
        if (botBehavior != null)
            botBehavior.OnBotStood += HandleBotStood;
    }

    private void OnDisable()
    {
        if (botBehavior != null)
            botBehavior.OnBotStood -= HandleBotStood;
    }

    private void HandleBotStood()
    {
        ShowIndicator(); // Включаем иконку "✋"
        // TODO: Проиграть анимацию жеста Stand через Animator
    }

    // При сбросе _hasBotStood (новый раунд) индикатор выключается автоматически
    // через подписку на OnValueChanged в базовом классе
}
