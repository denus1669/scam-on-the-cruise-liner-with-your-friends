using System;
using System.Collections.Generic;
using Assets.Casino.Cheating.MiniGames;
using Assets.Casino.Games;
using UnityEngine;

namespace Assets.Casino.Cheating
{
    [Flags]
    public enum CheatAudience
    {
        None   = 0,
        Player = 1 << 0,
        Bot    = 1 << 1,
        Both   = Player | Bot
    }

    public abstract class CheatAction : ScriptableObject
    {
        [Header("Идентификация")]
        public string CheatName = "Неизвестный мухлеж";

        [Header("Анимация")]
        [Tooltip("ID анимации для Animator (должен быть на префабе и игрока, и бота)")]
        public string AnimationTriggerName = "Cheat_Generic";

        [Tooltip("Длительность анимации для бота (время, за которое можно поймать за руку)")]
        public float AnimationDuration = 2.0f;

        [Header("Баланс")]
        [Tooltip("Вероятность для бота. Для игрока это поле может игнорироваться или влиять на стоимость мухлежа.")]
        public int SelectionWeight = 10;

        [Header("Мини-игры")]
        [Tooltip("Список мини-игр для этого чита. При срабатывании одна выбирается случайно (реализация выбора — отдельно).")]
        [SerializeField] private List<MinigameType> minigameTypes  = new();

        public IReadOnlyList<MinigameType> MinigameTypes => minigameTypes;

        public abstract bool CanExecute(IGameTable table, string who);
        public abstract void ApplyCheatResult(IGameTable table, string who);
    }
}