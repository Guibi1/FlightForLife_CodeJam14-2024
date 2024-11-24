using TMPro;
using UnityEngine;

public class MoveDrone : MonoBehaviour
{
    [Header("Movement Settings")]
    public float startDelay = 0f;         // Optional delay before starting the movement
    public float speed = 16f;         // Optional delay before starting the movement

    private Vector3 pausedPosition;
    private LTDescr pathDescr;
    private LTDescr overrideDescr;
    private string overrideId;


    void Start()
    {
    }

    // Initiates the ping-pong movement
    public void StartPath(Vector3 targetPosition)
    {
        pathDescr = LeanTween.move(gameObject, targetPosition, 0f)
           .setEase(LeanTweenType.easeInOutSine)
           .setSpeed(speed / 2)
           .setLoopPingPong(); // This creates a continuous forward and backward loop
    }

    public void PauseScanMovements()
    {
        pathDescr.pause();

    }

    public void ResumeScanMovements()
    {
        if (overrideDescr != null)
        {
            overrideId = null;
            overrideDescr.setOnComplete(() => { });
            LeanTween.cancel(overrideDescr.id);
            overrideDescr = LeanTween.move(gameObject, pausedPosition, 0f)
                .setEase(LeanTweenType.easeInOutSine)
                .setSpeed(speed).setOnComplete(() => { pathDescr.resume(); overrideDescr = null; });
        }
        else
        {
            pathDescr.resume();
        }
    }

    public void MoveDroneTo(Vector3 xy, string id)
    {
        if (overrideDescr != null)
        {
            LeanTween.cancel(overrideDescr.id);
            overrideDescr = null;
        }
        else
        {
            pathDescr.pause();
            pausedPosition = transform.position;
        }

        xy.y = gameObject.transform.position.y;
        overrideId = id;
        overrideDescr = LeanTween.move(gameObject, xy, 0f)
            .setEase(LeanTweenType.easeInOutSine).setSpeed(speed);
    }

    public string GetOverrideId()
    {
        return overrideId;
    }
}
