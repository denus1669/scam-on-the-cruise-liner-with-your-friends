    using UnityEngine;
    using UnityEngine.UI;
    namespace Assets.Casino.Cheating.MiniGames.MG_Slots.Wire
    {
        [System.Serializable]
        public class NodeConnection
        {
            public RectTransform start;
            public RectTransform finish;
        
            [Header("Визуал финиша")]
            public Image finishImage;
            public Color idleColor   = new Color(0.5f, 0.5f, 0.5f, 1f);
            public Color activeColor = Color.white;
        
            [System.NonSerialized] public bool isOccupied;
            
            public void SetVisual(bool occupied)
            {
                if (finishImage == null && finish != null)
                    finishImage = finish.GetComponentInChildren<Image>();
                if (finishImage != null)
                    finishImage.color = occupied ? activeColor : idleColor;
            }
        }
    }