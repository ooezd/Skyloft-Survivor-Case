using System.Collections;
using UnityEngine;

// Shared presentation only; Health and the callers still own death/game flow.
public class DeathAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float blendDuration = 0.08f;
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

    public IEnumerator Play()
    {
        foreach (Collider childCollider in GetComponentsInChildren<Collider>())
            childCollider.enabled = false;

        if (animator == null || !animator.HasState(0, DeathState))
            yield break;

        // A masked aiming/firing layer must not override the full-body death pose.
        for (int layer = 1; layer < animator.layerCount; layer++)
            animator.SetLayerWeight(layer, 0f);
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.CrossFadeInFixedTime(DeathState, blendDuration, 0, 0f);
        yield return null;
        while (animator != null && animator.isActiveAndEnabled)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == DeathState && state.normalizedTime >= 1f)
                yield break;
            yield return null;
        }
    }
}
