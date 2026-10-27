using UnityEngine;

namespace Assets.Casino.Cheating.MiniGames.MG_Slots.Mouse
{
    [System.Serializable]
    public class Hole
    {
        public RectTransform holeRect;
        
        [System.NonSerialized] public  MGMouseClick currentMouse;
        public bool IsOccupied => currentMouse != null;
    }
}