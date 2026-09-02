using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ExposureStage
{
    [Header("Уровень (1, 2 или 3)")]
    public int level;

    [Header("Объекты, которые должны появиться/включиться на этой стадии")]
    public List<GameObject> objectsToActivate = new();

    [Header("Объекты, которые должны исчезнуть/выключиться на этой стадии")]
    public List<GameObject> objectsToDeactivate = new();

    [Header("Дополнительные действия (анимации, звуки, включение пулемётов и т.д.)")]
    public UnityEvent onStageApplied;
}