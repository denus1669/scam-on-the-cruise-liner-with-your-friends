using UnityEngine;

public class StandIndicator : NetworkObjectVisibilityIndicator
{
    [SerializeField] private BaseBotBehaviour botBehaviour;

    protected override void Awake()
    {
        base.Awake();
        if (botBehaviour == null) botBehaviour = GetComponent<BaseBotBehaviour>();
    }

    private void OnEnable()
    {
        if (botBehaviour != null)
            botBehaviour.OnBotStoodChanged += HandleBotStoodChanged;
    }

    private void OnDisable()
    {
        if (botBehaviour != null)
            botBehaviour.OnBotStoodChanged -= HandleBotStoodChanged;
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
