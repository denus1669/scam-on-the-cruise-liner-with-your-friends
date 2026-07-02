using UnityEngine;

/// <summary>
/// 3D-индикатор состояния Stand бота.
/// Показывает иконку "✋" над головой бота, когда он закончил ход.
/// </summary>
public class StandBotIndicator : NetworkObjectVisibilityIndicator
{
    [Header("Зависимости")]
    [SerializeField] private BaseBotBehavior botBehavior;

    protected override void Awake()
    {
        base.Awake();

        if (botBehavior == null)
            botBehavior = GetComponent<BaseBotBehavior>();
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

        HideIndicator();
    }

    private void HandleBotStood()
    {
        ShowIndicator();
    }
}