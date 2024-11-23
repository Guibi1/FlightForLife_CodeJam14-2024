using UnityEngine;

public class MoveDrone : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveDistance = 10f;      // Distance to move forward
    public float moveDuration = 2f;       // Duration to move to the target position
    public float startDelay = 0f;         // Optional delay before starting the movement

    private Vector3 originalPosition;     // Original position of the GameObject
    private LTDescr tDescr;
    void Start()
    {
        // Store the original position
        originalPosition = transform.position;

       
        
        StartPingPong();
        
    }

    // Initiates the ping-pong movement
    void StartPingPong()
    {
        tDescr.toggle = true;
        Vector3 targetPosition = originalPosition + new Vector3(0, 0, moveDistance);

         tDescr =  LeanTween.move(gameObject, targetPosition, moveDuration)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong(); // This creates a continuous forward and backward loop
    }

    void OnPause()
    {
        tDescr.toggle = false;
   
    }

    void OnResume()
    {
        tDescr.toggle = true;
    }

}
