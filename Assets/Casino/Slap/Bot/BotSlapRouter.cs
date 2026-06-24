using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum BotSlapEventType { CheatSlapped, BluffSlapped, IdleSlapped }

/// <summary>
/// Маршрутизатор шлепков для бота с использованием паттерна "Стратегия".
/// </summary>
[RequireComponent(typeof(BotAgent))]
public class BotSlapRouter : NetworkBehaviour, ISlapTarget
{
    private BotSlapContext _context;
    private List<ISlapReaction<BotSlapContext>> _reactions;

    // Глобальные события (для анимаций, звуков, VFX на всех клиентах)
    public event Action OnCheatSlapped;
    public event Action OnBluffSlapped;
    public event Action OnIdleSlapped;

    private void Awake()
    {
        _context = new BotSlapContext
        {
            CheatController = GetComponent<CheatController>(),
            BotBehavior = GetComponent<BaseBotBehavior>(),
            DispleasureController = GetComponent<BotDispleasureController>(),
            TargetNetworkObject = GetComponent<NetworkObject>(),
            Router = this
        };

        // ПОРЯДОК ВАЖЕН: Роутер пойдет по списку сверху вниз.
        // Последней должна быть IdleSlapReaction (так как она всегда возвращает true).
        _reactions = new List<ISlapReaction<BotSlapContext>>
        {
            new BotCheatSlapReaction(),
            new BotBluffSlapReaction(),
            new BotIdleSlapReaction()
        };
    }

    public void ExecuteSlap(ulong slapperClientId)
    {
        if (!IsServer)
        {
            RequestSlapServerRpc(slapperClientId);
            return;
        }

        ResolveSlap(slapperClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestSlapServerRpc(ulong slapperClientId) => ResolveSlap(slapperClientId);

    private void ResolveSlap(ulong slapperClientId)
    {
        // Проходим по всем стратегиям и ищем ту, которая подходит под текущее состояние
        foreach (var reaction in _reactions)
        {
            if (reaction.CanSlap(_context))
            {
                reaction.Slap(slapperClientId, _context);
                break;
            }
        }
    }

    // --- Сетевая синхронизация событий для визуальных эффектов ---

    [Rpc(SendTo.Everyone)]
    public void TriggerEventClientRpc(BotSlapEventType eventType)
    {
        switch (eventType)
        {
            case BotSlapEventType.CheatSlapped:
                OnCheatSlapped?.Invoke();
                break;
            case BotSlapEventType.BluffSlapped:
                OnBluffSlapped?.Invoke();
                break;
            case BotSlapEventType.IdleSlapped:
                OnIdleSlapped?.Invoke();
                break;
        }
    }
}