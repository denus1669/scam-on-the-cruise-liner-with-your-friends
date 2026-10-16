using System.Collections.Generic;
using Assets.Casino.Games;
using UnityEngine;

namespace Assets.Casino.Cheating
{
    [CreateAssetMenu(menuName = "Casino/Cheating/Cheat Groups Library", fileName = "CheatGroupsLibrary")]
    public class CheatGroupsLibrary : ScriptableObject
    {
        [Tooltip("Все группы читов проекта.")]
        [SerializeField] private List<CheatGroup> allGroups = new();

        public IReadOnlyList<CheatGroup> AllGroups => allGroups;

        /// <summary>Читы, подходящие под тип стола и аудиторию.</summary>
        public IEnumerable<CheatAction> GetCheats(GameTable gameType, CheatAudience audience)
        {
            foreach (var group in allGroups)
            {
                if (group == null) continue;
                if ((audience & ~group.Audience) != 0) continue;

                var resolvedTable = group.ResolveTableType();
                if (gameType != null && resolvedTable != null && resolvedTable != gameType) continue;

                foreach (var cheat in group.Cheats)
                    if (cheat != null) yield return cheat;
            }
        }

        /// <summary>Все читы конкретной группы (включая вложенные).</summary>
        public IEnumerable<CheatAction> GetCheats(CheatGroup group, bool includeDescendants = true)
        {
            if (group == null) yield break;

            foreach (var candidate in allGroups)
            {
                if (candidate == null) continue;
                if (includeDescendants ? candidate.IsDescendantOf(group) : candidate == group)
                {
                    foreach (var cheat in candidate.Cheats)
                        if (cheat != null) yield return cheat;
                }
            }
        }

        public void SetGroups(List<CheatGroup> groups) => allGroups = groups;
    }
}