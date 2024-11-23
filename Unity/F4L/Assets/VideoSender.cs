using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;

public class VideoSender : MonoBehaviour
{
    public Camera sourceCamera; // The Camera to capture from

    // Start is called before the first frame update
    void Start()
    {
#if WEBRTC_3_0_0_PRE_5_OR_BEFORE
        WebRTC.Initialize();
#endif
        Debug.Log("WebRTC: Initialize ok");
        
        var track = sourceCamera.CaptureStreamTrack(1280, 720);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
