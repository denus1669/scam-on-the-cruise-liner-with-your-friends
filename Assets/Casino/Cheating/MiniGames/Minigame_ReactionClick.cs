using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простая мини-игра: нужно нажать кнопку в момент, когда ползунок находится в "зеленой зоне".
/// </summary>
public class Minigame_ReactionClick : CheatMinigameBase
{
    [Header("Настройки Реакции")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float targetMin = 0.4f;
    [SerializeField] private float targetMax = 0.6f;

    // UI элементы для визуализации "зеленой зоны" (настраивается в инспекторе)
    [SerializeField] private RectTransform successZoneVisual;

    private bool isPlaying = false;
    private bool movingRight = true;

    protected override void OnGameStarted()
    {
        progressSlider.value = 0f;
        movingRight = true;
        isPlaying = true;

        // В зависимости от "веса" мухлежа (SelectionWeight) можно менять сложность (скорость)
        // float difficultyModifier = currentCheatContext.SelectionWeight * 0.1f;
    }

    private void Update()
    {
        if (!isPlaying) return;

        // Движение ползунка туда-сюда
        float step = speed * Time.deltaTime;
        progressSlider.value += movingRight ? step : -step;

        if (progressSlider.value >= 1f) movingRight = false;
        if (progressSlider.value <= 0f) movingRight = true;

        // Игрок нажимает Space или кликает мышью
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            CheckResult();
        }
    }

    private void CheckResult()
    {
        isPlaying = false;
        float finalValue = progressSlider.value;

        if (finalValue >= targetMin && finalValue <= targetMax)
        {
            Debug.Log("[Minigame] Идеальный тайминг! Мухлеж удался.");
            WinMinigame();
        }
        else
        {
            Debug.Log("[Minigame] Промах! Игрок спалился.");
            LoseMinigame();
        }
    }
}