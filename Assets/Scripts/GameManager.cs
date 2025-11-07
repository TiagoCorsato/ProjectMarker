using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int NumOfAttempts = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        PauseState(true);
        Marker.Instance.attemptMade.AddListener(OnAttemptMade);
    }

    public void PauseState(bool IsPaused)
    {
        if (!IsPaused)
        {
            Controller.Instance.enabled = true;
            Marker.Instance.enabled = true;
        }
        else
        {
            Controller.Instance.enabled = false;
            Marker.Instance.enabled = false;
        }
    }

    public void OnAttemptMade()
    {
        NumOfAttempts++;
        UIController.Instance.UpdateAttemptCounter(NumOfAttempts);
    }
}
