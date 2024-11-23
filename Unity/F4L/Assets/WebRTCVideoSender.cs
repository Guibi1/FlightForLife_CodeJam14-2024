using UnityEngine;
using System.Net;
using System;
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
        public Camera sourceCamera;

        [HideInInspector] public RenderTexture renderTexture;
        [HideInInspector] public Texture2D texture;
        [HideInInspector] public Rect captureRect;
        [HideInInspector] public ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();
    }

    [Header("Camera Settings")]
    [Tooltip("List of cameras to stream. Ensure exactly 5 cameras are assigned.")]
    public List<CameraStream> cameras = new List<CameraStream>();

    [Header("Server Settings")]
    public int serverPort = 8080;
    public int resolutionX = 1280;
    public int resolutionY = 720;

    private HttpListener httpListener;
    private Thread listenerThread;
    private bool isRunning = false;

    void Start()
    {
        if (cameras.Count != 5)
        {
            Debug.LogError("CameraStreamServer: Exactly 5 cameras must be assigned in the Inspector.");
            return;
        }

        InitializeCameraStreams();
        StartServer();
    }

    void Update()
    {
        if (!isRunning) return;

        foreach (var camStream in cameras)
        {
            UpdateCameraStream(camStream);
        }
    }

    private void InitializeCameraStreams()
    {
        foreach (var camStream in cameras)
        {
            if (camStream.sourceCamera == null)
            {
                Debug.LogError("CameraStreamServer: One or more cameras are not assigned.");
                continue;
            }

            if (camStream.sourceCamera.targetTexture == null)
            {
                var rt = new RenderTexture(resolutionX, resolutionY, 0);
                rt.Create();
                camStream.sourceCamera.targetTexture = rt;
                camStream.renderTexture = rt;
            }
            else
            {
                camStream.renderTexture = camStream.sourceCamera.targetTexture;
            }

            camStream.texture = new Texture2D(resolutionX, resolutionY, TextureFormat.RGB24, false);
            camStream.captureRect = new Rect(0, 0, resolutionX, resolutionY);
        }
    }

    private void UpdateCameraStream(CameraStream camStream)
    {
        if (camStream.sourceCamera == null || camStream.renderTexture == null) return;

        try
        {
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = camStream.renderTexture;

            camStream.texture.ReadPixels(camStream.captureRect, 0, 0);
            camStream.texture.Apply();

            RenderTexture.active = currentRT;

            byte[] frameBytes = camStream.texture.EncodeToPNG(); // Lossless encoding
            camStream.frameQueue.Enqueue(frameBytes);

            while (camStream.frameQueue.Count > 10) camStream.frameQueue.TryDequeue(out _);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UpdateCameraStream Error: {ex.Message}");
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

            Debug.Log($"Server started on port {serverPort}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"StartServer Error: {ex.Message}");
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
                break;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"HandleRequests Error: {ex.Message}");
                continue;
            }

            ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
        }
    }

    private async void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string[] segments = request.RawUrl.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 2 && segments[0].Equals("cameras", System.StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(segments[1], out int cameraIndex) && cameraIndex >= 0 && cameraIndex < cameras.Count)
            {
                var camStream = cameras[cameraIndex];

                if (camStream.sourceCamera != null)
                {
                    response.ContentType = "multipart/x-mixed-replace; boundary=frame";
                    response.StatusCode = (int)HttpStatusCode.OK;

                    try
                    {
                        var outputStream = response.OutputStream;

                        while (isRunning && outputStream.CanWrite)
                        {
                            if (camStream.frameQueue.TryDequeue(out byte[] frameBytes))
                            {
                                string header = "--frame\r\nContent-Type: image/jpeg\r\n\r\n";
                                string footer = "\r\n";
                                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                                byte[] footerBytes = Encoding.UTF8.GetBytes(footer);

                                byte[] messageBytes = new byte[headerBytes.Length + frameBytes.Length + footerBytes.Length];
                                Buffer.BlockCopy(headerBytes, 0, messageBytes, 0, headerBytes.Length);
                                Buffer.BlockCopy(frameBytes, 0, messageBytes, headerBytes.Length, frameBytes.Length);
                                Buffer.BlockCopy(footerBytes, 0, messageBytes, headerBytes.Length + frameBytes.Length, footerBytes.Length);

                                await outputStream.WriteAsync(messageBytes, 0, messageBytes.Length);

                                Thread.Sleep(33); // ~30 FPS
                            }
                            else
                            {
                                Thread.Sleep(5); // Briefly wait if no frame is available
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"ProcessRequest Error: {ex.Message}");
                    }
                    finally
                    {
                        response.Close();
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    byte[] errorBytes = Encoding.UTF8.GetBytes("Camera not initialized");
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.Close();
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                byte[] errorBytes = Encoding.UTF8.GetBytes("Invalid camera index");
                await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                response.Close();
            }
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            byte[] errorBytes = Encoding.UTF8.GetBytes("Invalid request");
            await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
            response.Close();
        }
    }

    private void StopServer()
    {
        if (!isRunning) return;

        isRunning = false;

        try
        {
            httpListener?.Close();
            httpListener = null;

            listenerThread?.Abort();
            listenerThread = null;

            Debug.Log("Server stopped");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"StopServer Error: {ex.Message}");
        }
    }

    private void OnApplicationQuit() => StopServer();
    private void OnDestroy() => StopServer();
}
