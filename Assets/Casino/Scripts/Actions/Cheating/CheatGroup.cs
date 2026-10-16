using System.Collections.Generic;
using Assets.Casino.Games;
using UnityEngine;

namespace Assets.Casino.Cheating
{
    [CreateAssetMenu(menuName = "Casino/Cheating/Cheat Group", fileName = "CheatGroup_")]
    public class CheatGroup : ScriptableObject
    {
        [Tooltip("Название группы. Если пусто — берётся имя ассета.")]
        public string GroupName;
        [Header("Читы группы")]
        [Tooltip("Список читов, принадлежащих этой группе.")]
        [SerializeField] private List<CheatAction> cheats = new();

        [Tooltip("Родительская группа (иерархия). Может быть null.")]
        public CheatGroup Parent;

        [Tooltip("Тип стола, для которого актуальна группа. Если пусто — наследуется от Parent.")]
        public GameTableDefinition  GameType;

        [Tooltip("Для кого доступны читы этой группы.")]
        public CheatAudience Audience = CheatAudience.Both;
        

        public IReadOnlyList<CheatAction> Cheats => cheats;

        public string ResolvedName => string.IsNullOrEmpty(GroupName) ? name : GroupName;

        /// <summary>Ищет GameType вверх по иерархии.</summary>
        public GameTableDefinition ResolveTableType()
        {
            var cheatGroup = this;
            while (cheatGroup != null)
            {
                if (cheatGroup.GameType != null) return cheatGroup.GameType;
                cheatGroup = cheatGroup.Parent;
            }
            return null;
        }

        /// <summary>Все предки, включая себя.</summary>
        public IEnumerable<CheatGroup> SelfAndAncestors()
        {
            var cheatGroup = this;
            while (cheatGroup != null) { yield return cheatGroup; cheatGroup = cheatGroup.Parent; }
        }

        /// <summary>Является ли группа потомком target (или самим target).</summary>
        public bool IsDescendantOf(CheatGroup target)
        {
            foreach (var cheatGroup in SelfAndAncestors())
                if (cheatGroup == target) return true;
            return false;
        }

        /// <summary>Собирает читы этой группы и всех вложенных (рекурсивно).</summary>
        public IEnumerable<CheatAction> CollectCheatsWithDescendants(CheatGroupsLibrary library)
        {
            if (library == null) yield break;
            foreach (var group in library.AllGroups)
            {
                if (group == null || !group.IsDescendantOf(this)) continue;
                foreach (var cheat in group.Cheats)
                    if (cheat != null) yield return cheat;
            }
        }
    }
}