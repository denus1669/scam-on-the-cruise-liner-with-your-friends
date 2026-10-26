namespace Assets.Casino.Cheating.MiniGames.MG_Slots.Wire
{
    using UnityEngine;

    [System.Serializable]
    public class NodeConnection
    {
        public RectTransform start;
        public RectTransform finish;
        
        [System.NonSerialized] public bool isOccupied;

    }
}