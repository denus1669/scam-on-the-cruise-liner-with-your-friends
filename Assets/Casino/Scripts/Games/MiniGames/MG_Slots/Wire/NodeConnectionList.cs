using System;

namespace Assets.Casino.Cheating.MiniGames.MG_Slots.Wire
{
    using System.Collections.Generic;
    using UnityEngine;

    public class NodeConnectionList : MonoBehaviour
    {
        public List<NodeConnection > connections = new List<NodeConnection>();
        
        public event Action OnConnectionChanged;

        public NodeConnection FindByStart(RectTransform start)
        {
            for (int i = 0; i < connections.Count; i++)
                if (connections[i].start == start) return connections[i];
            return null;
        }
        
        public bool AllOccupied()
        {
            if (connections.Count == 0) return false;
            for (int i = 0; i < connections.Count; i++)
                if (!connections[i].isOccupied) return false;
            return true;
        }
        
        public void NotifyChanged()
        {
            Debug.Log("WD@ED");
            OnConnectionChanged?.Invoke();
        }
    }
}