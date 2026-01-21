using UnityEngine;

public class RespawnScreen : MonoBehaviour
{
    [SerializeField] private GameObject screen;
    
    public void ShowScreen() => screen.SetActive(true);
    public void HideScreen() => screen.SetActive(false);
}
