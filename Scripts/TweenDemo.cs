using UnityEngine;

public class TweenDemo : MonoBehaviour
{
    public CanvasGroup demoCanvasGroup;
    public Transform demoScaleTarget;

    void Start()
    {
        // Example: simple float tween
        var h1 = this.TweenFloat(0f, 1f, 1f, v => Debug.Log($"Float: {v:F2}"), () => Debug.Log("Float done"), UnityTween.EaseInOutQuad);

        // Example: fade out then scale sequence
        Func<TweenHandle> fadeOut = () => this.FadeCanvasGroup(demoCanvasGroup, 1f, 0f, 0.5f, null, UnityTween.EaseOutCubic);
        Func<TweenHandle> scaleDown = () => this.Scale(demoScaleTarget, demoScaleTarget.localScale, demoScaleTarget.localScale * 0.5f, 0.5f, null, UnityTween.EaseOutBack);

        var seq = this.Sequence(new System.Func<TweenHandle>[] { fadeOut, scaleDown }, () => Debug.Log("Sequence complete"));

        // Example parallel: fade in while scaling up
        Func<TweenHandle> fadeIn = () => this.FadeCanvasGroup(demoCanvasGroup, demoCanvasGroup.alpha, 1f, 0.6f, null, UnityTween.EaseOutCubic);
        Func<TweenHandle> scaleUp = () => this.Scale(demoScaleTarget, demoScaleTarget.localScale, Vector3.one, 0.6f, null, UnityTween.EaseOutElastic);

        var par = this.Parallel(new System.Func<TweenHandle>[] { fadeIn, scaleUp }, () => Debug.Log("Parallel complete"));
    }
}
