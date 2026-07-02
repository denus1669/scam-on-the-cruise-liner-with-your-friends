using UnityEngine;

/// <summary>
/// Глобальная заглушка анимации блефа бота.
/// Видна ВСЕМ игрокам. В будущем будет заменена на настоящую анимацию.
/// </summary>
public class BluffAnimationIndicator : NetworkObjectVisibilityIndicator
{
    [Header("Зависимости")]
    [SerializeField] private BotBluffController bluffController;

    protected override void Awake()
    {
        base.Awake();

        if (bluffController == null)
            bluffController = GetComponent<BotBluffController>();
    }

    private void OnEnable()
    {
        if (bluffController != null)
            bluffController.OnBluffStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (bluffController != null)
            bluffController.OnBluffStateChanged -= HandleStateChanged;

        HideIndicator();
    }
}
