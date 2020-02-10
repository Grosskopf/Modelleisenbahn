using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move_along_path : MonoBehaviour
{
    public Transform Train;
    public double MoveSpeed=0.1;
    public double DistanceTraveled = 0;

    // Start is called before the first frame update
    List<Vector3> positions = new List<Vector3>();
    List<double> distances = new List<double>();
    public double maxdistance;

    void Start()
    {
        double currentdist = 0.0f;
        Vector3 currentpos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 firstpos = new Vector3(0.0f, 0.0f, 0.0f);
        bool isfirstpos = true;
        foreach (Transform child in transform)
        {
            positions.Add(child.position);
            if (isfirstpos)
            {
                isfirstpos = false;
                currentpos = child.position;
                firstpos = child.position;
            }
            else
            {
                currentdist += Vector3.Distance(currentpos, child.position);
                currentpos =child.position;
            }
            distances.Add(currentdist);
        }
        maxdistance = currentdist + Vector3.Distance(currentpos, firstpos);
    }

    // Update is called once per frame
    void Update()
    {
        DistanceTraveled += Time.deltaTime * MoveSpeed;
        if (DistanceTraveled > maxdistance)
        {
            DistanceTraveled -= maxdistance;
        }
        Vector3 betweenA = positions[positions.Count - 1];
        Vector3 betweenB = positions[0];
        Vector3 betweenC = positions[0];
        float ratio = (float)-1.0;
        for (int i = 1;i< positions.Count; i++){
            if (distances[i - 1] < DistanceTraveled && distances[i] > DistanceTraveled)
            {
                betweenA = positions[i - 1];
                betweenB = positions[i];
                ratio = (float)((DistanceTraveled - distances[i-1]) / (distances[i] - distances[i-1]));
                if (i < positions.Count - 1)
                {
                    betweenC = positions[i + 1];
                }
                break;
            }
        }
        if (ratio == -1.0)
        {
            ratio = (float)((DistanceTraveled - distances[distances.Count - 1]) / (maxdistance - distances[distances.Count - 1]));
            betweenC = positions[1];
        }
        Vector3 newTrainPos = Vector3.Lerp(betweenA, betweenB, ratio);
        Train.position = newTrainPos;
        Train.LookAt(Vector3.Lerp(betweenB, betweenC, ratio));

    }
}
