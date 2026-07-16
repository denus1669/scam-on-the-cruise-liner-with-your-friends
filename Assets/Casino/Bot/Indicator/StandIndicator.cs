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
            botBehavior.OnBotStoodChanged += HandleBotStoodChanged;
    }

    private void OnDisable()
    {
        if (botBehavior != null)
            botBehavior.OnBotStoodChanged -= HandleBotStoodChanged;
    }

    private void HandleBotStoodChanged(bool hasStood)
    {
        if (hasStood)
            ShowIndicator();
        else
            HideIndicator();
    }

    // При сбросе _hasBotStood (новый раунд) индикатор выключается автоматически
    // через подписку на OnValueChanged в базовом классе
}
