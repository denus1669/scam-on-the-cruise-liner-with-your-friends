using Assets.Casino.Bot;
using System.Linq;
using UnityEngine;

public class BotPlayGameIndicator : NetworkObjectVisibilityIndicator
{
    private BaseBotBehaviour[] _cachedBehaviours;

    protected override void Awake()
    {
        base.Awake();
        // Находим ВСЕ поведения на боте (включая выключенные)
        _cachedBehaviours = GetComponentsInParent<BaseBotBehaviour>(true);
    }

    private void OnEnable()
    {
        if (_cachedBehaviours == null || _cachedBehaviours.Length == 0) return;

        // Подписываемся на все
        foreach (var behaviour in _cachedBehaviours)
        {
            if (behaviour != null)
                behaviour.OnBotPlayChanged += HandleStateChanged;
        }

        // Синхронизируем состояние с активным поведением
        var activeBehaviour = _cachedBehaviours.FirstOrDefault(b => b != null && b.enabled);
        HandleStateChanged(activeBehaviour != null && activeBehaviour.IsPlaying);
    }

    private void OnDisable()
    {
        if (_cachedBehaviours == null) return;

        foreach (var behaviour in _cachedBehaviours)
        {
            if (behaviour != null)
                behaviour.OnBotPlayChanged -= HandleStateChanged;
        }
        HideIndicator();
    }
}