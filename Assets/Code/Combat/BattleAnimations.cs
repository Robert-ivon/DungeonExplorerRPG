using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleAnimations : MonoBehaviour
{
    public static IEnumerator AttackMove(RectTransform actor, Vector2 originalPos, float distance = 50f, float duration = 0.15f)
    {
        if (actor == null) yield break;

        // Movimiento hacia adelante
        Vector2 target = originalPos + Vector2.right * distance;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float step = Mathf.Clamp01(t);
            actor.anchoredPosition = Vector2.Lerp(originalPos, target, step);
            yield return null;
        }

        // Regreso
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float step = Mathf.Clamp01(t);
            actor.anchoredPosition = Vector2.Lerp(target, originalPos, step);
            yield return null;
        }

        // ensure final position stable
        actor.anchoredPosition = originalPos;
    }

    public static IEnumerator TakeDamageFlash(Image targetImage, Color flashColor, float flashDuration = 0.1f, int flashes = 2)
    {
        if (targetImage == null) yield break;

        Color originalColor = targetImage.color;
        for (int i = 0; i < flashes; i++)
        {
            targetImage.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            targetImage.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        // ensure original color restored
        targetImage.color = originalColor;
    }
}
