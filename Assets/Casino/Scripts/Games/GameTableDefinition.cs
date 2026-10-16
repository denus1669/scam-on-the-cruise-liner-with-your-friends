using System;
using UnityEngine;

namespace Assets.Casino.Games
{
    [CreateAssetMenu(menuName = "Casino/Games/Game Table Definition", fileName = "GameTable_")]
    public class GameTableDefinition : ScriptableObject
    {
        [Tooltip("Отображаемое имя стола.")]
        public string TableName;
    }
}