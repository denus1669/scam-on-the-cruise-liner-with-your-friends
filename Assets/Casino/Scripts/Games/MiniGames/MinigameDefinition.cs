using UnityEngine;

namespace Assets.Casino.Cheating.MiniGames
{
    [CreateAssetMenu(menuName = "Casino/Minigames/Minigame Definition", fileName = "Minigame_")]
    public class MinigameDefinition : ScriptableObject
    {
        [Tooltip("Отображаемое имя мини-игры.")]
        public string MinigameName;

        [Tooltip("Префаб с компонентом MinigameBase.")]
        [SerializeField] private MinigameBase minigamePrefab;
    }
}
