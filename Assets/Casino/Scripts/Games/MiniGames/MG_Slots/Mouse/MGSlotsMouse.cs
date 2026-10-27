using Assets.Casino.Cheating.MiniGames;
using Assets.Casino.Cheating.MiniGames.MG_Slots.Mouse;
using UnityEngine;

public class MGSlotsMouse : MinigameBase
{
    [Header("Ссылки")]
    [SerializeField] private HoleList holeList;
    [SerializeField] private MGMouseClick mousePrefab;

    [Header("Настройки")]
    [Tooltip("Максимум мышей. Реальное число = Random(1..min(maxMice, кол-во дырок))")]
    [SerializeField] private int maxMice = 6;

    private int _totalMice;
    private int _killed;

    protected override void GameInitializer(bool isPlaying)
    {
        if (holeList == null || mousePrefab == null)
        {
            Debug.LogWarning("[MGSlotsMouse] Не заданы holeList или mousePrefab", this);
            return;
        }

        holeList.ClearAll();
        _killed = 0;

        int limit = Mathf.Min(maxMice, holeList.holes.Count);
        if (limit <= 0) return;

        _totalMice = Random.Range(1, limit + 1);

        for (int i = 0; i < _totalMice; i++)
        {
            Hole h = holeList.GetRandomFreeHole();
            if (h == null) break;

            MGMouseClick mouse = Instantiate(mousePrefab, h.holeRect);
            mouse.OnKilled += HandleMouseKilled;
            mouse.StartLife(holeList, h);
        }
    }

    private void HandleMouseKilled(MGMouseClick mouse)
    {
        mouse.OnKilled -= HandleMouseKilled;
        _killed++;
        if (_killed >= _totalMice)
            WinGame();
    }

    public override void ForceClose()
    {
        base.ForceClose();

        if (holeList != null)
        {
            for (int i = 0; i < holeList.holes.Count; i++)
            {
                var m = holeList.holes[i].currentMouse;
                if (m != null) Destroy(m.gameObject);
                holeList.holes[i].currentMouse = null;
            }
        }
        _killed = 0;
        _totalMice = 0;
    }
}