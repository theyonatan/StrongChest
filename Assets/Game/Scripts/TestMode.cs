using UnityEngine;

public class TestMode : MonoBehaviour
{
    [SerializeField] private bool testMode;
    
    void Start()
    {
        if (!testMode)
            gameObject.SetActive(false);
    }
}
