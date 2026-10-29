using Assets.Casino.Cheating.MiniGames;
using Assets.Casino.Cheating.MiniGames.MG_Slots.Mouse;
using UnityEngine;

public class MGSlotsMousePanel : MinigameBase
{
    public override MinigameType MinigameKind => MinigameType.Slots;
    
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

        _totalMice = Random.Range(8, limit + 1);

        for (int i = 0; i < _totalMice; i++)
        {
            MGMouseClick mouse = Instantiate(mousePrefab, holeList.transform);
            mouse.OnKilled += HandleMouseKilled;
            mouse.StartLife(holeList);
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
            var all = holeList.GetComponentsInChildren<MGMouseClick>(true);
            for (int i = 0; i < all.Length; i++)
                Destroy(all[i].gameObject);
            holeList.ClearAll();
        }
        _killed = 0;
        _totalMice = 0;
    }
}