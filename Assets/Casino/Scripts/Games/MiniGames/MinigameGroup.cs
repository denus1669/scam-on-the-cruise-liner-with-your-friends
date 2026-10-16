using System.Collections.Generic;
using Assets.Casino.Games;
using UnityEngine;

namespace Assets.Casino.Cheating.MiniGames
{
    [CreateAssetMenu(menuName = "Casino/Minigames/Minigame Group", fileName = "MinigameGroup_")]
    public class MinigameGroup : ScriptableObject
    {
        [Tooltip("Название группы. Если пусто — берётся имя ассета.")]
        public string GroupName; 
        [Header("Мини-игры группы")]
        [SerializeField] private List<MinigameDefinition> minigames = new();
        
        public IReadOnlyList<MinigameDefinition> Minigames => minigames;

        public string ResolvedName => string.IsNullOrEmpty(GroupName) ? name : GroupName;
    }
}