using TMPro;
using UnityEngine;

public class MoveDrone : MonoBehaviour
{
    [SerializeField("Sweep area")]
    public float size = 10f;
    public ServerConnection sc;

    void Start()
    {
        double gap = size / sc.drones.Count;

        for (int i = 0; i < drones.Count; i++)
        {
            drones[i].transform.position = new Vector3(0, 10, gap * (i + 0.5));
            drones[i].GetComponent<MoveDrone>().StartPath(transform.position + new Vector3(size, 10, gap * (i + 0.5)));
        }
    }
}
