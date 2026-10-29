using System.Collections.Generic;
using UnityEngine;

namespace Assets.Casino.Cheating.MiniGames.MG_Slots.Wire
{
    public class MGSlotsWire : MinigameBase
    {
        public override MinigameType MinigameKind => MinigameType.Slots;

        [SerializeField] private NodeConnectionList nodesList;
        
        [SerializeField] private RectTransform[] startSlots;   // пустые маркеры
        [SerializeField] private RectTransform[] finishSlots;
        
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
            
            for (int i = 0; i < nodesList.connections.Count; i++)
                nodesList.connections[i].SetVisual(false);

            // Разблокировать все DragSource и погасить линии
            var sources = nodesList.GetComponentsInChildren<DragSource>(true);
            for (int i = 0; i < sources.Length; i++)
                sources[i].ResetState();

            ShuffleAndPlace();
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
        
        private void PlaceIntoSlot(RectTransform item, RectTransform slot)
        {
            item.SetParent(slot, false);
            item.anchoredPosition = Vector2.zero;
            item.localRotation = Quaternion.identity;
            item.localScale = Vector3.one;
        }

        private void Shuffle(RectTransform[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
        
        private void ShuffleAndPlace()
        {
            var starts = new List<RectTransform>(nodesList.connections.Count);
            var finishes = new List<RectTransform>(nodesList.connections.Count);
            for (int i = 0; i < nodesList.connections.Count; i++)
            {
                starts.Add(nodesList.connections[i].start);
                finishes.Add(nodesList.connections[i].finish);
            }
        
            Shuffle(startSlots);
            Shuffle(finishSlots);
        
            int ns = Mathf.Min(starts.Count, startSlots.Length);
            for (int i = 0; i < ns; i++)
                PlaceIntoSlot(starts[i], startSlots[i]);
        
            int nf = Mathf.Min(finishes.Count, finishSlots.Length);
            for (int i = 0; i < nf; i++)
                PlaceIntoSlot(finishes[i], finishSlots[i]);
        }
        
    }
}
