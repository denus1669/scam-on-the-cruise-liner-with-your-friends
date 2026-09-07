using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BotCheatController : CheatController
{
    [Header("Ссылки")]
    [SerializeField] private BotAgent botAgent;
    [SerializeField] private BotDispleasureController displeasureController;

    public override bool CanCheat()
    {
        // Проверяем, что персонаж не в процессе мухлежа и что он владелец (или бот)
        return !isCheating.Value;
    }

    public override bool TryInitiateCheat(IGameTable table, CheatAction specificCheat = null)
    {
        if (!IsServer || isCheating.Value) return false;

        currentTable = table;
        CheatAction cheatToExecute = specificCheat;

        // 2. Если мухлеж не указан явно (например, это ИИ), выбираем подходящий случайно
        if (cheatToExecute == null)
        {
            cheatToExecute = GetRandomValidCheat();
            if (cheatToExecute == null)
            {
                Debug.Log("Нет доступных вариантов мухлежа для текущей ситуации.");
                return false; // Нет доступных вариантов
            }
        }
        else
        {
            if (!cheatToExecute.CanExecute(currentTable, "BOT")) return false;
        }


        // 3. Запускаем процесс
        CheatRoutine(cheatToExecute);
        return true;
    }
    protected override void CheatRoutine(CheatAction cheat)
    {
        base.CheatRoutine(cheat);
        cheatCoroutine = StartCoroutine(BotCheatRoutine(cheat));
    }

    public IEnumerator BotCheatRoutine(CheatAction cheat)
    {

        // Бот просто ждёт AnimationDuration — это окно для обвинения игроком
        float timer = cheat.AnimationDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        // Мухлеж бота удался (игрок его не поймал)
        CompleteCheat(cheat, "BOT");
    }

    private CheatAction GetRandomValidCheat()
    {
        List<CheatAction> validCheats = new List<CheatAction>();
        int totalWeight = 0;

        foreach (var cheat in availableCheats)
        {
            if (cheat.CanExecute(currentTable, "BOT"))
            {
                validCheats.Add(cheat);
                totalWeight += cheat.SelectionWeight;
            }
        }

        if (validCheats.Count == 0) return null;

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        foreach (var cheat in validCheats)
        {
            randomValue -= cheat.SelectionWeight;
            if (randomValue < 0) return cheat;
        }

        return validCheats[validCheats.Count - 1];
    }


}
