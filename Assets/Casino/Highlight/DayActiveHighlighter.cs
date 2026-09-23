using Assets.Casino.Games.BlackGreg;
using Blocks.Gameplay.Core;
using System;
using UnityEngine;

namespace Assets.Casino.PhaseDay.GameStateActivate
{
    /// <summary>
    /// Подсветка объекта начала игрового дня.
    ///
    /// Отвечает только за правила доступности конкретного объекта.
    /// Визуал и сетевая синхронизация находятся в базовом классе InteractableHighlighterBase.
    /// </summary>
    [RequireComponent(typeof(DayActiveInteractable))]
    public class DayActiveHighlighter : HighlightControllerBase
    {

        private float _checkTimer;
        private void Reset()
        {
            // Автоматически подтягиваем ссылку в редакторе.
            _interactable = GetComponent<DayActiveInteractable>();
        }
        private void Update()
        {

            _checkTimer += Time.deltaTime;

            if (_checkTimer < availabilityCheckInterval)
                return;

            _checkTimer = 0f;

            SetAvailableHighlight(_interactable.HasAnyAvailableInteractor());
        }
    }
}