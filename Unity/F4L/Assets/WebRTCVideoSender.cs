using UnityEngine;
using System.Net;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class CameraStreamServer : MonoBehaviour
{
    [System.Serializable]
    public class CameraStream
    {
        [Tooltip("Assign your camera here.")]
        public Camera sourceCamera; // Assign your camera in the Inspector

        [HideInInspector]
        public RenderTexture renderTexture;

        [HideInInspector]
        public Texture2D texture;

        [HideInInspector]
        public Rect captureRect;

        [HideInInspector]
        public ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();
    }

    [Header("Camera Settings")]
    [Tooltip("List of cameras to stream. Ensure exactly 5 cameras are assigned.")]
    public List<CameraStream> cameras = new List<CameraStream>();

    [Header("Server Settings")]
    [Tooltip("Port number for the HTTP server.")]
    public int serverPort = 8080; // Port number for the HTTP server

    private HttpListener httpListener;
    private Thread listenerThread;
    private bool isRunning = false;

    void Start()
    {
        // Ensure exactly 5 cameras are assigned
        if (cameras.Count != 5)
        {
            Debug.LogError("CameraStreamServer: Exactly 5 cameras must be assigned in the Inspector.");
            return;
        }

        InitializeCameraStreams();

        // Start the HTTP server
        StartServer();
    }

    void Update()
    {
        if (!isRunning)
            return;

        foreach (var camStream in cameras)
        {
            UpdateCameraStream(camStream);
        }
    }

    private void InitializeCameraStreams()
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            CameraStream camStream = cameras[i];
            string camName = camStream.sourceCamera != null ? camStream.sourceCamera.name : $"Camera_{i}";

            if (camStream.sourceCamera == null)
            {
                Debug.LogError($"CameraStreamServer: Camera at index {i} is not assigned in the Inspector.");
                continue;
            }

            // Ensure the camera has a RenderTexture assigned
            if (camStream.sourceCamera.targetTexture == null)
            {
                // Create a new RenderTexture
                RenderTexture rt = new RenderTexture(1280, 720, 24);
                rt.Create();
                camStream.sourceCamera.targetTexture = rt;
                camStream.renderTexture = rt;
                Debug.Log($"CameraStreamServer: Assigned a new RenderTexture to {camName}.");
            }
            else
            {
                camStream.renderTexture = camStream.sourceCamera.targetTexture;
                Debug.Log($"CameraStreamServer: {camName} already has a RenderTexture assigned.");
            }

            // Initialize the Texture2D and capture rect based on the RenderTexture
            RenderTexture rtAssigned = camStream.renderTexture;
            if (rtAssigned == null)
            {
                Debug.LogError($"CameraStreamServer: {camName}'s RenderTexture is still null after assignment.");
                continue;
            }

            int width = rtAssigned.width;
            int height = rtAssigned.height;
            camStream.texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            camStream.captureRect = new Rect(0, 0, width, height);
        }
    }

    private void UpdateCameraStream(CameraStream camStream)
    {
        if (camStream.sourceCamera == null || camStream.renderTexture == null)
            return;

        try
        {
            // Capture the camera's RenderTexture each frame
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = camStream.renderTexture;

            // Read the pixels from the RenderTexture
            camStream.texture.ReadPixels(camStream.captureRect, 0, 0);
            camStream.texture.Apply();

            RenderTexture.active = currentRT;

            // Encode the texture to JPG
            byte[] frameBytes = camStream.texture.EncodeToJPG();

            // Enqueue the frame for streaming
            camStream.frameQueue.Enqueue(frameBytes);

            // Limit the queue size to avoid memory issues
            while (camStream.frameQueue.Count > 10)
            {
                camStream.frameQueue.TryDequeue(out _);
            }
        }
        catch (System.Exception ex)
        {
            string camName = camStream.sourceCamera != null ? camStream.sourceCamera.name : "Unknown Camera";
            Debug.LogError($"CameraStreamServer Update Error for {camName}: {ex.Message}");
        }
    }

    private void StartServer()
    {
        try
        {
            isRunning = true;
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://*:{serverPort}/");
            httpListener.Start();

            listenerThread = new Thread(HandleRequests);
            listenerThread.Start();

            Debug.Log($"CameraStreamServer: HTTP server started on port {serverPort}");
            for (int i = 0; i < cameras.Count; i++)
            {
                if (cameras[i].sourceCamera != null)
                {
                    Debug.Log($"CameraStreamServer: Stream available at http://<your_ip>:{serverPort}/cameras/{i}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CameraStreamServer StartServer Error: {ex.Message}");
        }
    }

    private void HandleRequests()
    {
        while (isRunning)
        {
            HttpListenerContext context = null;

            try
            {
                context = httpListener.GetContext();
            }
            catch (HttpListenerException)
            {
                // Listener was closed
                break;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CameraStreamServer HandleRequests Exception: {ex.Message}");
                continue;
            }

            // Handle the request in a separate thread
            ThreadPool.QueueUserWorkItem((_) => ProcessRequest(context));
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        // Parse the URL to determine which camera stream to serve
        string[] segments = request.RawUrl.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Expected endpoint format: /cameras/{index}
        if (segments.Length == 2 && segments[0].Equals("cameras", System.StringComparison.OrdinalIgnoreCase))
        {
            string indexStr = segments[1];
            if (int.TryParse(indexStr, out int cameraIndex))
            {
                if (cameraIndex >= 0 && cameraIndex < cameras.Count)
                {
                    CameraStream camStream = cameras[cameraIndex];

                    if (camStream != null && camStream.sourceCamera != null)
                    {
                        response.ContentType = "multipart/x-mixed-replace; boundary=frame";
                        response.StatusCode = (int)HttpStatusCode.OK;

                        try
                        {
                            Stream outputStream = response.OutputStream;
                            while (isRunning && outputStream.CanWrite)
                            {
                                if (camStream.frameQueue.TryDequeue(out byte[] frameBytes))
                                {
                                    string header = "--frame\r\nContent-Type: image/jpeg\r\n\r\n";
                                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                                    byte[] footerBytes = Encoding.UTF8.GetBytes("\r\n");

                                    outputStream.Write(headerBytes, 0, headerBytes.Length);
                                    outputStream.Write(frameBytes, 0, frameBytes.Length);
                                    outputStream.Write(footerBytes, 0, footerBytes.Length);
                                    outputStream.Flush();

                                    // Sleep to control frame rate (approximately 30 FPS)
                                    Thread.Sleep(3);
                                }
                                else
                                {
                                    // No frame available, wait a bit
                                    Thread.Sleep(10);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"CameraStreamServer ProcessRequest Error for Camera Index {cameraIndex}: {ex.Message}");
                        }
                        finally
                        {
                            response.Close();
                        }
                    }
                    else
                    {
                        // Handle 404 Not Found for uninitialized camera
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        byte[] errorBytes = Encoding.UTF8.GetBytes("404 Not Found: Camera not initialized");
                        response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                        response.Close();
                    }
                }
                else
                {
                    // Handle 404 Not Found for invalid camera index
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    byte[] errorBytes = Encoding.UTF8.GetBytes("404 Not Found: Camera index out of range");
                    response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                    response.Close();
                }
            }
            else
            {
                // Handle 400 Bad Request for non-integer index
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                byte[] errorBytes = Encoding.UTF8.GetBytes("400 Bad Request: Invalid camera index");
                response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                response.Close();
            }
        }
        else
        {
            // Handle 404 Not Found for invalid URLs
            response.StatusCode = (int)HttpStatusCode.NotFound;
            byte[] errorBytes = Encoding.UTF8.GetBytes("404 Not Found");
            response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
            response.Close();
        }
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void StopServer()
    {
        if (!isRunning)
            return;

        isRunning = false;

        try
        {
            if (httpListener != null)
            {
                httpListener.Close();
                httpListener = null;
            }

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Abort();
                listenerThread = null;
            }

            Debug.Log("CameraStreamServer: HTTP server stopped");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CameraStreamServer StopServer Error: {ex.Message}");
        }
    }
}
