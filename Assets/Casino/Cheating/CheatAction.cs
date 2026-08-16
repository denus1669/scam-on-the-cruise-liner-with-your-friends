using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Базовый класс для любого вида мухлежа.
/// Подходит для ботов.
/// </summary>
public abstract class CheatAction : ScriptableObject
{
    [Header("Базовые настройки мухлежа")]
    public string CheatName = "Неизвестный мухлеж";

    [Tooltip("ID анимации для Animator (должен быть на префабе и игрока, и бота)")]
    public string AnimationTriggerName = "Cheat_Generic";

    [Tooltip("Длительность анимации (время, за которое можно поймать за руку)")]
    public float AnimationDuration = 2.0f;

    [Tooltip("Вероятность для ИИ. Для игрока это поле может игнорироваться или влиять на стоимость мухлежа.")]
    public int SelectionWeight = 10;

    /// <summary>
    /// Проверяет, можно ли сейчас выполнить этот мухлеж.
    /// Вызывается только на СЕРВЕРЕ.
    /// </summary>
    public abstract bool CanExecute(IGameTable table);

    /// <summary>
    /// Применяет фактический результат мухлежа (подмена карт, изменение счета).
    /// Вызывается на СЕРВЕРЕ, если мухлеж не был прерван/раскрыт.
    /// </summary>
    public abstract void ApplyCheatResult(IGameTable table);
}