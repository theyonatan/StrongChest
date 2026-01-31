using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class KillChatEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI killedText;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float fadeoutDuration = 0.7f;

    public UnityEvent OnEntryDestroyed = new ();

    public void SetKilled(string killer, string victim)
    {
        killedText.text = $"  {killer} +=={{:::::::::::::::::> {victim}";
        StartCoroutine(LifeCycle());
    }

    private IEnumerator LifeCycle()
    {
        yield return new WaitForSeconds(lifeTime - fadeoutDuration);
        yield return FadeAndMoveOut();
        Destroy(gameObject);
    }

    private IEnumerator FadeAndMoveOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeoutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeoutDuration;

            float eased = Mathf.SmoothStep(0f, 1f, t);
            canvasGroup.alpha = 1f - eased;

            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    public void ForceRemove()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        
        OnEntryDestroyed?.Invoke();
        OnEntryDestroyed?.RemoveAllListeners();
    }
    
    public static string EmojiToUnicode(string emoji)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < emoji.Length; i++)
        {
            int codePoint = char.ConvertToUtf32(emoji, i);

            // Skip surrogate pair extra step
            if (codePoint > 0xFFFF)
                i++;

            sb.Append($"\\U{codePoint:X8}");
        }

        return sb.ToString();
    }
}
