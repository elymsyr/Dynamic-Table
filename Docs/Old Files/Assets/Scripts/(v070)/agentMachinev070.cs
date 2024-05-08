using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;
using System.Diagnostics;
using UnityEngine.UIElements;
using System;

public class ChildObjectManagerv070 : Agent
{
    // [SerializeField]
    // private bool switchBehavior = false;
    [SerializeField]
    private bool showUI = false;
    private GameObject parentObject;
    private TextMeshPro ui;
    private int rows = 15;
    private int columns = 15;
    private Vector3 target_start;
    private Vector3 product_start;
    private float minY = 0f;
    private float maxY = 4f;
    [SerializeField]
    private float move_speed = 1f;
    [SerializeField]
    private float distance_lim = 4.35f;
    private Transform[,] childArray;
    private Rigidbody productRigidbody;
    [SerializeField]
    private int actionLimit = 900;
    private int actionCount = 0;
    private Vector3[,] tableLoc;
    private int gameCount = 0;
    private float legalY;
    private float startDistance;
    [SerializeField]
    private GameObject env;
    [SerializeField]
    private GameObject product;
    [SerializeField]
    private GameObject target;
    private int win = 0;
    [SerializeField]
    private GameObject text;
    private float delta;
    private float distance;
    private float deltaDistance = 0;
    private List<int[]> coordinates;
    private List<int[]> neighborCoordinates;
    private List<int[]> outerCoordinates;
    private List<int[]> usedCoors;
    private List<int[]> possibleCoors;
    private void Awake()
    {
        coordinates = new List<int[]>();
        neighborCoordinates = new List<int[]>();
        outerCoordinates = new List<int[]>();
        usedCoors = new List<int[]>();
        possibleCoors = new List<int[]>();
        if (text != null)
        {
            ui = text.GetComponent<TextMeshPro>();
        }
        productRigidbody = product.GetComponent<Rigidbody>();
        legalY = product.transform.localPosition.y;
        parentObject = transform.gameObject;
        if (parentObject != null)
        {
            GetChildObjects();
        }
        else
        {
            UnityEngine.Debug.LogError("Parent object not assigned!");
        }
    }
    private void GetChildObjects()
    {
        childArray = new Transform[rows, columns];
        tableLoc = new Vector3[rows, columns];
        int index = 0;

        foreach (Transform child in parentObject.transform)
        {
            int i = index / rows;
            int j = index % columns;
            childArray[i, j] = child;
            tableLoc[i, j] = child.transform.localPosition;
            index++;
        }
    }
    
    private void updateUI()
    {
        ui.text = "Product States\nPosition: "+product.transform.position+"\nDistance to Target: "+targetCloseness()+"\nReward: "+GetCumulativeReward()+"\nDelta: "+delta+"\nAction Count: "+actionCount+"\nGame Count: "+gameCount+"\nWin Count: "+win;
    }
    public void triggerReset(){
        AddReward(-2f);
        EndEpisode();
    }
    public void winReset(){
        win++;
        AddReward(2f);
        EndEpisode();
    }
    private float targetCloseness()
    {
        float distance = Vector3.Distance(product.transform.localPosition, target.transform.localPosition);
        return (float)Math.Round(distance - (int)distance, 2) + (int)distance;
    }
    private bool GetDistanceToChild(Transform child)
    {
        float distance = Vector3.Distance(env.transform.InverseTransformPoint(new Vector3(child.position.x, 0f, child.position.z)), env.transform.InverseTransformPoint(new Vector3(product.transform.position.x, 0f, product.transform.position.z)));
        return distance < distance_lim;
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (actionLimit < actionCount)
        {
            AddReward(-1f);
            EndEpisode();
        }
        actionCount++;

        coordinates.Clear();
        neighborCoordinates.Clear();
        outerCoordinates.Clear();
        usedCoors.Clear();
        possibleCoors.Clear();
        for (int k = 0; k < 4; k++)
        {
            coordinates.Add(new int[] { actions.DiscreteActions[k], actions.DiscreteActions[k+4] });
            usedCoors.Add(new int[] { actions.DiscreteActions[k], actions.DiscreteActions[k+4] });
        }
        
        foreach (var coord in coordinates)
        {
            int i = coord[0];
            int j = coord[1];

            if (i - 1 >= 0 && j - 1 >= 0 && !usedCoors.Contains(new int[] { i - 1, j - 1 })) neighborCoordinates.Add(new int[] { i - 1, j - 1 });
            if (i - 1 >= 0 && j - 1 >= 0 && !usedCoors.Contains(new int[] { i - 1, j - 1 })) usedCoors.Add(new int[] { i - 1, j - 1 });

            if (i - 1 >= 0 && j + 1 < 15 && !usedCoors.Contains(new int[] { i - 1, j + 1 })) neighborCoordinates.Add(new int[] { i - 1, j + 1 });
            if (i - 1 >= 0 && j + 1 < 15 && !usedCoors.Contains(new int[] { i - 1, j + 1 })) usedCoors.Add(new int[] { i - 1, j + 1 });

            if (i + 1 < 15 && j - 1 >= 0 && !usedCoors.Contains(new int[] { i + 1, j - 1 })) neighborCoordinates.Add(new int[] { i + 1, j - 1 });
            if (i + 1 < 15 && j - 1 >= 0 && !usedCoors.Contains(new int[] { i + 1, j - 1 })) usedCoors.Add(new int[] { i + 1, j - 1 });

            if (i + 1 < 15 && j + 1 < 15 && !usedCoors.Contains(new int[] { i + 1, j + 1 })) neighborCoordinates.Add(new int[] { i + 1, j + 1 });
            if (i + 1 < 15 && j + 1 < 15 && !usedCoors.Contains(new int[] { i + 1, j + 1 })) usedCoors.Add(new int[] { i + 1, j + 1 });
        }

        foreach (var coord in coordinates)        
        {
            int i = coord[0];
            int j = coord[1];


            if (i - 1 >= 0 && !usedCoors.Contains(new int[] { i - 1, j })) neighborCoordinates.Add(new int[] { i - 1, j });
            if (i - 1 >= 0 && !usedCoors.Contains(new int[] { i - 1, j })) usedCoors.Add(new int[] { i - 1, j });

            if (i + 1 < 15 && !usedCoors.Contains(new int[] { i + 1, j })) neighborCoordinates.Add(new int[] { i + 1, j });
            if (i + 1 < 15 && !usedCoors.Contains(new int[] { i + 1, j })) usedCoors.Add(new int[] { i + 1, j });

            if (j + 1 < 15 && !usedCoors.Contains(new int[] { i, j + 1 })) neighborCoordinates.Add(new int[] { i, j + 1 });
            if (j + 1 < 15 && !usedCoors.Contains(new int[] { i, j + 1 })) usedCoors.Add(new int[] { i, j + 1 });

            if (j - 1 >= 0 && !usedCoors.Contains(new int[] { i, j - 1 })) neighborCoordinates.Add(new int[] { i, j - 1 });
            if (j - 1 >= 0 && !usedCoors.Contains(new int[] { i, j - 1 })) usedCoors.Add(new int[] { i, j - 1 });
        }

        foreach (var coord in coordinates)
        {
            int i = coord[0];
            int j = coord[1];

            if (i-2 >= 0 && j-2 >= 0 && !usedCoors.Contains(new int[] { i-2, j-2 })) outerCoordinates.Add(new int[] { i-2, j-2 });
            if (i-2 >= 0 && j-2 >= 0 && !usedCoors.Contains(new int[] { i-2, j-2 })) usedCoors.Add(new int[] { i-2, j-2 });

            if (i-2 >= 0 && j-1 >= 0 && !usedCoors.Contains(new int[] { i-2, j-1 })) outerCoordinates.Add(new int[] { i-2, j-1 });
            if (i-2 >= 0 && j-1 >= 0 && !usedCoors.Contains(new int[] { i-2, j-1 })) usedCoors.Add(new int[] { i-2, j-1 });

            if (i-2 >= 0 && j >= 0 && !usedCoors.Contains(new int[] { i-2, j })) outerCoordinates.Add(new int[] { i-2, j });
            if (i-2 >= 0 && j >= 0 && !usedCoors.Contains(new int[] { i-2, j })) usedCoors.Add(new int[] { i-2, j });             

            if (i-2 >= 0 && j+1 <= 14 && !usedCoors.Contains(new int[] { i-2, j+1 })) outerCoordinates.Add(new int[] { i-2, j+1 });
            if (i-2 >= 0 && j+1 <= 14 && !usedCoors.Contains(new int[] { i-2, j+1 })) usedCoors.Add(new int[] { i-2, j+1 });

            if (i-2 >= 0 && j+2 <= 14 && !usedCoors.Contains(new int[] { i-2, j+2 })) outerCoordinates.Add(new int[] { i-2, j+2 });
            if (i-2 >= 0 && j+2 <= 14 && !usedCoors.Contains(new int[] { i-2, j+2 })) usedCoors.Add(new int[] { i-2, j+2 });

            if (i+2 <= 14 && j-2 >= 0 && !usedCoors.Contains(new int[] { i+2, j-2 })) outerCoordinates.Add(new int[] { i+2, j-2 });
            if (i+2 <= 14 && j-2 >= 0 && !usedCoors.Contains(new int[] { i+2, j-2 })) usedCoors.Add(new int[] { i+2, j-2 });

            if (i+2 <= 14 && j-1 >= 0 && !usedCoors.Contains(new int[] { i+2, j-1 })) outerCoordinates.Add(new int[] { i+2, j-1 });
            if (i+2 <= 14 && j-1 >= 0 && !usedCoors.Contains(new int[] { i+2, j-1 })) usedCoors.Add(new int[] { i+2, j-1 });

            if (i+2 <= 14 && j >= 0 && !usedCoors.Contains(new int[] { i+2, j })) outerCoordinates.Add(new int[] { i+2, j });
            if (i+2 <= 14 && j >= 0 && !usedCoors.Contains(new int[] { i+2, j })) usedCoors.Add(new int[] { i+2, j });             

            if (i+2 <= 14 && j+1 <= 14 && !usedCoors.Contains(new int[] { i+2, j+1 })) outerCoordinates.Add(new int[] { i+2, j+1 });
            if (i+2 <= 14 && j+1 <= 14 && !usedCoors.Contains(new int[] { i+2, j+1 })) usedCoors.Add(new int[] { i+2, j+1 });

            if (i+2 <= 14 && j+2 <= 14 && !usedCoors.Contains(new int[] { i+2, j+2 })) outerCoordinates.Add(new int[] { i+2, j+2 });
            if (i+2 <= 14 && j+2 <= 14 && !usedCoors.Contains(new int[] { i+2, j+2 })) usedCoors.Add(new int[] { i+2, j+2 });  

            if (i-1 >= 1 && j-2 >= 0 && !usedCoors.Contains(new int[] { i-1, j-2 })) outerCoordinates.Add(new int[] { i-1, j-2 });
            if (i-1 >= 1 && j-2 >= 0 && !usedCoors.Contains(new int[] { i-1, j-2 })) usedCoors.Add(new int[] { i-1, j-2 });        

            if (i >= 1 && j-2 >= 0 && !usedCoors.Contains(new int[] { i, j-2 })) outerCoordinates.Add(new int[] { i, j-2 });
            if (i >= 1 && j-2 >= 0 && !usedCoors.Contains(new int[] { i, j-2 })) usedCoors.Add(new int[] { i, j-2 });  

            if (i+1 <= 14 && j-2 >= 0 && !usedCoors.Contains(new int[] { i+1, j-2 })) outerCoordinates.Add(new int[] { i+1, j-2 });
            if (i+1 <= 14 && j-2 >= 0 && !usedCoors.Contains(new int[] { i+1, j-2 })) usedCoors.Add(new int[] { i+1, j-2 });     

            if (i-1 >= 1 && j+2 <= 14 && !usedCoors.Contains(new int[] { i-1, j+2 })) outerCoordinates.Add(new int[] { i-1, j+2 });
            if (i-1 >= 1 && j+2 <= 14 && !usedCoors.Contains(new int[] { i-1, j+2 })) usedCoors.Add(new int[] { i-1, j+2 });        

            if (i >= 0 && j+2 <= 14 && !usedCoors.Contains(new int[] { i, j+2 })) outerCoordinates.Add(new int[] { i, j+2 });
            if (i >= 0 && j+2 <= 14 && !usedCoors.Contains(new int[] { i, j+2 })) usedCoors.Add(new int[] { i, j+2 });  

            if (i+1 <= 14 && j+2 <= 14 && !usedCoors.Contains(new int[] { i+1, j+2 })) outerCoordinates.Add(new int[] { i+1, j+2 });
            if (i+1 <= 14 && j+2 <= 14 && !usedCoors.Contains(new int[] { i+1, j+2 })) usedCoors.Add(new int[] { i+1, j+2 });                                                    

        }

        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                if (!usedCoors.Contains(new int[] { i, j })){possibleCoors.Add(new int[] { i, j });}
            }
        }

        foreach (var coord in possibleCoors){
            float newYPosition = childArray[coord[0], coord[1]].localPosition.y + -2.3f * Time.deltaTime * move_speed;
            newYPosition = Mathf.Clamp(newYPosition, minY, maxY);
            childArray[coord[0], coord[1]].localPosition = new Vector3(childArray[coord[0], coord[1]].localPosition.x, newYPosition, childArray[coord[0], coord[1]].localPosition.z);            
        }

        foreach (var coord in outerCoordinates){
            float newYPosition = childArray[coord[0], coord[1]].localPosition.y + 1.2f * Time.deltaTime * move_speed;
            newYPosition = Mathf.Clamp(newYPosition, minY, maxY);
            childArray[coord[0], coord[1]].localPosition = new Vector3(childArray[coord[0], coord[1]].localPosition.x, newYPosition, childArray[coord[0], coord[1]].localPosition.z);              
        }

        foreach (var coord in neighborCoordinates){
            float newYPosition = childArray[coord[0], coord[1]].localPosition.y + 2f * Time.deltaTime * move_speed;
            newYPosition = Mathf.Clamp(newYPosition, minY, maxY);
            childArray[coord[0], coord[1]].localPosition = new Vector3(childArray[coord[0], coord[1]].localPosition.x, newYPosition, childArray[coord[0], coord[1]].localPosition.z);              
        }

        foreach (var coord in coordinates){
            float newYPosition = childArray[coord[0], coord[1]].localPosition.y + 3f * Time.deltaTime * move_speed;
            newYPosition = Mathf.Clamp(newYPosition, minY, maxY);
            childArray[coord[0], coord[1]].localPosition = new Vector3(childArray[coord[0], coord[1]].localPosition.x, newYPosition, childArray[coord[0], coord[1]].localPosition.z);              
        }
 

        // int index = 0;
        // foreach (Transform child in childArray)
        // {
        //     if (child != null)
        //     {
        //         if (switchBehavior || GetDistanceToChild(child)){
        //             float newYPosition = child.localPosition.y + actions.ContinuousActions[index] * move_speed * Time.deltaTime;
        //             newYPosition = Mathf.Clamp(newYPosition, minY, maxY);
        //             child.localPosition = new Vector3(child.localPosition.x, newYPosition, child.localPosition.z);
        //         }
        //         else
        //         {
        //             int i = index / rows;
        //             int j = index % columns;
        //             child.transform.localPosition = tableLoc[i, j];
        //         }

        //         index++;
        //     }
        //     else{
        //         UnityEngine.Debug.Log("Null child founded!");
        //     }
        // }

        distance = targetCloseness();
        delta = deltaDistance - distance;
        if (showUI)
        {
            updateUI();
        }
        deltaDistance = distance;
        AddReward(delta*Mathf.Abs(startDistance-distance)/startDistance);
    }

    public override void OnEpisodeBegin()
    {
        productRigidbody.velocity = Vector3.zero;
        do {
            target_start = randomPos();
            product_start = randomPos();
        } while (Vector3.Distance(target_start, product_start) < 5);

        target.transform.localPosition = target_start;
        product.transform.localPosition = product_start;
        startDistance = Vector3.Distance(target_start, product_start);

        actionCount = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                childArray[i,j].transform.localPosition = tableLoc[i,j];
            }
        }
        gameCount++;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (Transform child in childArray)
        {
            if (child != null)
            {
                sensor.AddObservation(child.localPosition.y);
            }
        }

        sensor.AddObservation(product.transform.localPosition);
        sensor.AddObservation(targetCloseness());
        sensor.AddObservation(-43.83164f);
        sensor.AddObservation(-22.29164f);
        sensor.AddObservation(18.97933f);
        sensor.AddObservation(-3.260668f);
    }
    private Vector3 randomPos(){
        return new Vector3(UnityEngine.Random.Range(-39.52f, -26.7f), legalY, UnityEngine.Random.Range(1.51f, 14.05f));
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        for (int i = 0; i < 4; i++)
        {
            continuousActions[i] = UnityEngine.Random.Range(-1f, 1f);
        }
        for (int i = 0; i < 8; i++)
        {
            System.Random rnd = new System.Random();
            discreteActions[i] = rnd.Next(0,15);
        }
    }

}
