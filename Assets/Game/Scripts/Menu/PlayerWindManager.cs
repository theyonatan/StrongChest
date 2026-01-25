using System;
using UnityEngine;

/// <summary>
/// What moves between scenes locally
/// (not attached to network behavior or anything multiplayer)
/// </summary>
public class PlayerWindManager : MonoBehaviour
{
    public static PlayerWindManager Instance { get; private set; }

    #region Singleton

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    public static string ChosenUsername = "";
}
