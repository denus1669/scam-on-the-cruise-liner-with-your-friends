using System;
using Assets.Casino.Cheating.MiniGames;
using Casino.Scripts.Actions.Cheating.MiniGames;
using UnityEngine;

public class ExitReached : MonoBehaviour
{
    [Header("Select MiniGame")]
    [SerializeField] private MinigameBase _minigameBase;
    [Header("What must happen if ExitReached")]
    [SerializeField] private bool isWin = false;
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(isWin);
        if (isWin)
            _minigameBase.WinGame();
        if (!isWin)
            _minigameBase.LoseGame();
    }
}
