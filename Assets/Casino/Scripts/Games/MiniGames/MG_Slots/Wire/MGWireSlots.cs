using UnityEngine;

namespace Assets.Casino.Cheating.MiniGames.MG_Slots.Wire
{
    public class MGSlotsWire : MinigameBase
    {
        [SerializeField] private NodeConnectionList nodesList;
        
        protected override void OnGameStarted()
        {
            base.OnGameStarted();
            if (nodesList != null)
                nodesList.OnConnectionChanged += CheckWin;
        }

        protected override void GameInitializer(bool isPlaying)
        {
            if (nodesList == null) return;

            for (int i = 0; i < nodesList.connections.Count; i++)
                nodesList.connections[i].isOccupied = false;

            // Разблокировать все DragSource и погасить линии
            var sources = nodesList.GetComponentsInChildren<DragSource>(true);
            for (int i = 0; i < sources.Length; i++)
                sources[i].ResetState();
        }

        private void CheckWin()
        {
            Debug.Log($"_isPlaying && nodesList.AllOccupied() {_isPlaying} $$$ {nodesList.AllOccupied()}");
            if (_isPlaying && nodesList.AllOccupied())
                WinGame();
        }

        public override void ForceClose()
        {
            if (nodesList != null)
                nodesList.OnConnectionChanged -= CheckWin;
            base.ForceClose();
        }
        
    }
}
