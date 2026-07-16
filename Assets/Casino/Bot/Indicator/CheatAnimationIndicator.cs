using UnityEngine;

/// <summary>
/// Глобальная заглушка анимации мухлежа бота.
/// Видна ВСЕМ игрокам. В будущем будет заменена на настоящую анимацию.
/// </summary>
/// 

public class CheatAnimationIndicator : NetworkObjectVisibilityIndicator
{
    [Header("Зависимости")]
    [SerializeField] private CheatController cheatController;

    protected override void Awake()
    {
        base.Awake();

        if (cheatController == null)
            cheatController = GetComponent<CheatController>();
    }

    private void OnEnable()
    {
        if (cheatController != null)
            cheatController.OnCheatingStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (cheatController != null)
            cheatController.OnCheatingStateChanged -= HandleStateChanged;

        HideIndicator();
    }
}
