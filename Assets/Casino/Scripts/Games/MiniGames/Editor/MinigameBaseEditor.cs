using UnityEditor;
using UnityEngine;
using Assets.Casino.Cheating.MiniGames;

namespace Assets.Casino.Cheating.MiniGames.Editor
{
    [CustomEditor(typeof(MinigameBase), true)] // true — для наследников
    public class MinigameBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Тестирование", EditorStyles.boldLabel);

            MinigameBase minigame = (MinigameBase)target;

            if (GUILayout.Button("▶ Start Game (тест)", GUILayout.Height(30)))
            {
                minigame.StartGame("editor_test", success =>
                {
                    Debug.Log($"[MinigameBase] Игра завершена. Успех: {success}");
                });
            }

            if (GUILayout.Button("✖ Force Close", GUILayout.Height(24)))
            {
                minigame.ForceClose();
            }

            if (GUILayout.Button("✔ Win Game", GUILayout.Height(24)))
            {
                minigame.WinGame();
            }

            if (GUILayout.Button("✘ Lose Game", GUILayout.Height(24)))
            {
                minigame.LoseGame();
            }
        }
    }
}