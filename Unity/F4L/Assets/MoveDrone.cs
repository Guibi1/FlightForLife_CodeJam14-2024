using TMPro;
using UnityEngine;

public class MoveDrone : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveDistance = 10f;      // Distance to move forward
    public float moveDuration = 2f;       // Duration to move to the target position
    public float startDelay = 0f;         // Optional delay before starting the movement

    private Vector3 originalPosition;     // Original position of the GameObject
    private Vector3 pausedPosition;
    private LTDescr pathDescr;
    private LTDescr overrideDescr;
    private string overrideId;


    void Start()
    {
        originalPosition = transform.position;
        StartPath();
    }

    // Initiates the ping-pong movement
    void StartPath()
    {
        Vector3 targetPosition = originalPosition + new Vector3(0, 0, moveDistance);

        pathDescr = LeanTween.move(gameObject, targetPosition, moveDuration)
           .setEase(LeanTweenType.easeInOutSine)
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
            overrideDescr = LeanTween.move(gameObject, pausedPosition, 20f)
                .setEase(LeanTweenType.easeInOutSine)
                .setSpeed(2f).setOnComplete(() => { pathDescr.resume(); overrideDescr = null; });
        }
        else
        {
            pathDescr.resume();
        }
    }

    public void MoveDroneTo(Vector2 xy, string id)
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

        overrideId = id;
        float moveSpeed = 2f;
        overrideDescr = LeanTween.move(gameObject, xy, moveSpeed)
            .setEase(LeanTweenType.easeInOutSine).setSpeed(moveSpeed);
    }

    public string GetOverrideId()
    {
        return overrideId;
    }
}
