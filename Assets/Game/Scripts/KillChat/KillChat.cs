using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillChat : MonoBehaviour
{
    #region Singleton
    
    public static KillChat Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region KillChatEvents

    [SerializeField] private KillChatEntry entryPrefab;
    [SerializeField] private Image background;
    
    private readonly Queue<KillChatEntry> _entries = new ();
    private const int MaxEntries = 6;

    private void Start()
    {
        // Hide on start
        Color backgroundColor = background.color;
        backgroundColor.a = 0f;
        background.color = backgroundColor;
    }

    public void AddKill(string shooter, string killed)
    {
        var entry = Instantiate(entryPrefab, transform);
        entry.SetKilled(shooter, killed);
        entry.OnEntryDestroyed.AddListener(OnEntryRemoved);
    
        _entries.Enqueue(entry);
        
        if (_entries.Count == 1)
            FadeBackground(0.4196f);

        if (_entries.Count > MaxEntries)
        {
            var old = _entries.Dequeue();
            old.ForceRemove();
        }
    }

    #endregion

    #region Test

    private static int i = 1;
    public void TestKill()
    {
        i++;
        AddKill($"{i} Someone", "Someone Else");
    }

    #endregion

    #region BackgroundTransparency

    private void OnEntryRemoved()
    {
        _entries.Dequeue();
        
        if (_entries.Count == 0)
            FadeBackground(0f);
    }

    private void FadeBackground(float alphaTarget)
    {
        StopAllCoroutines();
        
        StartCoroutine(FadeRoutine(alphaTarget));
    }
    
    private IEnumerator FadeRoutine(float alphaTarget)
    {
        Color backgroundColor = background.color;
        float start = backgroundColor.a;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            backgroundColor.a = Mathf.Lerp(start, alphaTarget, t);
            background.color = backgroundColor;
            yield return null;
        }
        
        backgroundColor.a = alphaTarget;
        background.color = backgroundColor;
    }

    #endregion
}
