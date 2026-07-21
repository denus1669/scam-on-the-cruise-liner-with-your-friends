using UnityEngine;
using Blocks.Gameplay.Core;
using System;


public class AttentionGameListener : MonoBehaviour
{
    [SerializeField] private GameEvent onAttentionEvent;
    [SerializeField] private PlayerAttentionController playerAttentionController;

    private void OnEnable() => onAttentionEvent?.RegisterListener(OnAttentionActivated);

    private void OnDisable() => onAttentionEvent?.UnregisterListener(OnAttentionActivated);


    private void OnAttentionActivated()
    {
        playerAttentionController?.ToggleAttentionLocal();
    }
}
