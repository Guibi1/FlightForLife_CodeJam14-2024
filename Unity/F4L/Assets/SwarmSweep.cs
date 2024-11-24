using TMPro;
using UnityEngine;

public class SwarmSweep : MonoBehaviour
{
    public float size = 10f;
    public ServerConnection sc;

    void Start()
    {
        double gap = size / sc.drones.Count;

        for (int i = 0; i < sc.drones.Count; i++)
        {
            Vector3 pos = new Vector3(-22, 10, (float)(gap * (i + 0.5)));
            sc.drones[i].transform.position = pos;
            sc.drones[i].GetComponent<MoveDrone>().StartPath(pos + new Vector3(size, 0, 0));
        }
    }
}
