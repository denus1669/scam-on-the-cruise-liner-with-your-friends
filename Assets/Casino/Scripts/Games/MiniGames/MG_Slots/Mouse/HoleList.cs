using System.Collections.Generic;
using Assets.Casino.Cheating.MiniGames.MG_Slots.Mouse;
using UnityEngine;

public class HoleList : MonoBehaviour
{
    public List<Hole> holes = new List<Hole>();

    public Hole GetRandomFreeHole()
    {
        var free = new List<Hole>();
        for (int i = 0; i < holes.Count; i++)
            if (!holes[i].IsOccupied) free.Add(holes[i]);
        if (free.Count == 0) return null;
        return free[Random.Range(0, free.Count)];
    }

    public void ClearAll()
    {
        for (int i = 0; i < holes.Count; i++)
            holes[i].currentMouse = null;
    }
}