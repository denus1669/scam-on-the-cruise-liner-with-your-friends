using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Implement on objects that can be targeted by the Attention Raycaster.
    /// </summary>
    public interface IAttentionTarget
    {
        /// <summary>
        /// Called when the attention reticle enters this target.
        /// </summary>
        void OnAttentionEnter(GameObject instigator);

        /// <summary>
        /// Called when the attention reticle exits this target.
        /// </summary>
        void OnAttentionExit(GameObject instigator);

        /// <summary>
        /// Called when the player presses the Accuse key while targeting this object.
        /// </summary>
        void OnAccuse(GameObject instigator);
    }
}