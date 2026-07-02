using Blocks.Gameplay.Core;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum BotSlapEventType { CheatSlapped, BluffSlapped, IdleSlapped }

/// <summary>
/// Маршрутизатор шлепков для бота с использованием паттерна "Стратегия".
/// </summary>
[RequireComponent(typeof(BotAgent))]
public class BotSlapRouter : NetworkBehaviour, IInteractable
{
    [SerializeField] private CasinoBank casinoBank;
    public CasinoBank CasinoBank => casinoBank;

    private BotSlapContext _context;
    private List<ISlapReaction<BotSlapContext>> _reactions;

    [SerializeField] private string promptText = "Шлёпнуть по рукам (E)";


    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 10;
    public string InteractionPromptText => promptText;

    public float HoldDuration => 0f;


    // Глобальные события (для анимаций, звуков, VFX на всех клиентах)
    public event Action OnCheatSlapped;
    public event Action OnBluffSlapped;
    public event Action OnIdleSlapped;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (casinoBank == null && IsServer)
        {
            casinoBank = FindAnyObjectByType<CasinoBank>();
            if (casinoBank == null)
            {
                Debug.LogWarning($"[BotSlapRouter] {gameObject.name}: CasinoBank не найден. " +
                                 $"Штрафы за шлепки не будут работать.");
            }
        }
    }


    private void Awake()
    {
        _context = new BotSlapContext
        {
            CheatController = GetComponent<CheatController>(),
            BluffController = GetComponent<BotBluffController>(),
            BotBehavior = GetComponent<BaseBotBehavior>(),
            DispleasureController = GetComponent<BotDispleasureController>(),
            TargetNetworkObject = GetComponent<NetworkObject>(),
            Router = this,
            BotAgent = GetComponent<BotAgent>()  
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

    public bool CanInteract(GameObject interactor) => true; // Бота можно шлепать всегда

    public void Interact(GameObject interactor)
    {
        ulong slapperId = interactor.GetComponent<NetworkObject>().OwnerClientId;
        ExecuteSlap(slapperId);
    }
}