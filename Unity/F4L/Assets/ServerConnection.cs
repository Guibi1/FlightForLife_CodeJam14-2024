using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Collections;

public class ServerConnection : MonoBehaviour
{
    private SocketIOUnity socket;

    [SerializeField]
    private GameObject[] drones;

    private const double originLatitude = 37.926337; 
    private const double originLongitude = -122.612707;
    private const double scaleFactor = 0.00001;


    void Start()
    {
        Debug.Log("Initializing Socket.IO client...");
        var uri = new System.Uri("http://localhost:5000/unity");
        socket = new SocketIOUnity(uri);

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Connected to Socket.IO server!");

            SendMessageToServer("Hello from Unity!");
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("Disconnected from server: " + e);
        };

        socket.On("message", response =>
        {
            Debug.Log("Message from server: " + response.GetValue<string>());
        });

        socket.On("move_command", response =>
        {
            Debug.Log("Move_Command from server: " + response.GetValue<DroneWebPositionData>());

            DroneData droneData = WebPosToDroneData(response.GetValue<DroneWebPositionData>(), originLatitude, originLongitude, scaleFactor);
            MoveDroneTo(droneData);
        });

        Debug.Log("Attempting to connect...");
        socket.Connect();
    }

    void Update()
    {
        SendDronePositionsToServer();
    }

    public void SendDronePositionsToServer()
    {
        if (socket.Connected)
        {
            List<DroneWebPositionData> droneDataList = new List<DroneWebPositionData>();

            for (int i = 0; i < drones.Length; i++)
            {
                DroneWebPositionData droneData = DroneToWebPos(i, originLatitude,originLongitude,scaleFactor);
                droneDataList.Add(droneData);
            }

            string json = JsonConvert.SerializeObject(droneDataList, Formatting.Indented);
            socket.Emit("positions", json);

        }
        else
        {
            Debug.LogWarning("Cannot send positions. Not connected to server.");
        }
    }
    public void MoveDroneTo(DroneData droneData){
        Vector3 targetPosition = new Vector3(droneData.pos.x, droneData.pos.y, droneData.pos.z);
        float moveSpeed = 2f;

        LeanTween.move(gameObject, targetPosition,moveSpeed)
            .setEase(LeanTweenType.easeInOutSine)
            .setSpeed(moveSpeed);
    }

    // Function to emit a message to the server
    public void SendMessageToServer(string message)
    {
        if (socket.Connected)
        {


            Debug.Log($"Sending message to server: {message}");
            socket.Emit("message", message);
        }
        else
        {
            Debug.LogWarning("Cannot send message. Not connected to server.");
        }
    }

    public DroneData WebPosToDroneData(DroneWebPositionData droneWebData, double originLatitude, double originLongitude, double scaleFactor)
    {
        if (droneWebData == null)
        {
            throw new ArgumentNullException("DroneWebPositionData cannot be null.");
        }

        // Extract latitude and longitude
        double latitude = droneWebData.lat;
        double longitude = droneWebData.lng;

        // Convert latitude and longitude to local x, y coordinates
        double y = (latitude - originLatitude) / scaleFactor;
        double x = (longitude - originLongitude) * Math.Cos(originLatitude * Math.PI / 180) / scaleFactor;

        // Return a new DroneData object with converted values
        return new DroneData
        {
            pos = new PositionData
            {
                x = (float)x,
                y = (float)y,
                z = drones[droneWebData.id].transform.position.z, // Default Z value (can be updated if needed)
                rotation = droneWebData.rotation // Default rotation (can be updated if needed)
            }
        };
    }



    public DroneWebPositionData DroneToWebPos(int i, double originLatitude, double originLongitude, double scaleFactor)
    {
        GameObject drone = drones[i];

        // Extract x and y from the DroneData object
        double x = drone.transform.position.x;
        double y = drone.transform.position.y;

        // Convert x, y to latitude and longitude
        double latitude = originLatitude + (y * scaleFactor);
        double longitude = originLongitude + (x * scaleFactor / Math.Cos(originLatitude * Math.PI / 180));

        // Return a new DroneWebPositionData object with converted values
        return new DroneWebPositionData
        {
            id = i, // Default ID or customize as needed
            lng = (float)longitude,
            lat = (float)latitude,
            rotation = drone.transform.eulerAngles.z,
        };
    }

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
        public float rotation;
    }

    [System.Serializable]
    public class DroneData
    {
        public PositionData pos;
    }

    public class DroneWebPositionData
    {
        public int id;
        public float lng;
        public float lat;
        public float rotation;
      
    }

}

