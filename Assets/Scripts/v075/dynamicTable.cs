using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents;
using TMPro;
using Unity.MLAgents.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;
using Unity.Barracuda;

public class dynamicTable075 : Agent
{
    [SerializeField] private bool showUI = false;
    [SerializeField] private bool randomTableSize = false;
    [SerializeField] private GameObject text;
    private TextMeshPro ui;
    private float directionPoint = 0;
    public Vector3 oldLocation = new Vector3(0f,0f,0f);
    Vector3 newLocation = new Vector3(0f,0f,0f);
    private int win = 0;
    private float[] wallBorders;
    private CreateBoard075 table;
    private int rows;
    private int columns;
    public Transform[,] boxesArray;
    public Vector3[,] boxesLoc;
    private GameObject product;
    private GameObject target;
    [SerializeField] [Range(7,8)] private int size = 8;
    [Range(0f,15f)] public float MoveSpeed = 10f;
    private Transform[] activeArray;
    private Rigidbody productRigidbody;
    private List<Tuple<int, int>> specifiedPoints;
    private bool onTarget = false;
    private productCollision075 productClass;
    private rayRotation rayComp;
    private List<float> observation;
    private RayPerceptionOutput.RayOutput[] rayOutputs;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;


    void Awake()
    {
        observation = new List<float>(10);
        if (MaxStep<1){
            MaxStep = 600;
        }
        if (size == 7){ // 45 Observation 37 Actions
            specifiedPoints = new List<Tuple<int, int>>(){new Tuple<int, int>(0, 0),new Tuple<int, int>(0, 1),new Tuple<int, int>(1, 0),new Tuple<int, int>(0, 5),new Tuple<int, int>(0, 6),new Tuple<int, int>(1, 6),new Tuple<int, int>(5, 0),new Tuple<int, int>(6, 0),new Tuple<int, int>(6, 1),new Tuple<int, int>(6, 5),new Tuple<int, int>(6, 6),new Tuple<int, int>(5, 6)};
        }
        else{ // 52 Observation 55 Actions
            specifiedPoints = new List<Tuple<int, int>>(){new Tuple<int, int>(0, 0),new Tuple<int, int>(0, 1),new Tuple<int, int>(1, 0),new Tuple<int, int>(2, 0),new Tuple<int, int>(0, 2),new Tuple<int, int>(0, 5),new Tuple<int, int>(0, 6),new Tuple<int, int>(0, 7),new Tuple<int, int>(1, 7),new Tuple<int, int>(2, 7),new Tuple<int, int>(5, 0),new Tuple<int, int>(6, 0),new Tuple<int, int>(7, 0),new Tuple<int, int>(7, 1),new Tuple<int, int>(7, 2),new Tuple<int, int>(7, 5),new Tuple<int, int>(7, 6),new Tuple<int, int>(7, 7),new Tuple<int, int>(6, 7),new Tuple<int, int>(5, 7),};
        }

        if (text != null)
        {
            ui = text.GetComponent<TextMeshPro>();
        }        
        table = transform.GetComponent<CreateBoard075>();
        rows = table.rows;
        columns = table.columns;
        foreach (Transform child in table.transform)
        {
            Destroy(child.gameObject);
        }
        table.CreateEnv();
        product = table.getProduct;
        Transform ray = product.transform.GetChild(0);
        rayPerceptionSensor = ray.GetComponent<RayPerceptionSensorComponent3D>();
        target = table.getTarget;


        if (product == null){Debug.Log("Product NULL");}
        if (target == null){Debug.Log("Target NULL");}
        productRigidbody = product.GetComponent<Rigidbody>();
        boxesArray = table.getPieces;
        rows = table.rows;
        columns = table.columns;
        wallBorders = table.getBorders;
        productClass = product.GetComponent<productCollision075>();
        productClass.InitializeProduct(table.wallsArray,target,gameObject);
        boxesLoc = new Vector3[rows,columns];     
        for(int i=0; i<rows;i++){
            for(int j=0;j<columns;j++){
                boxesLoc[i,j]=boxesArray[i,j].transform.localPosition;
            }
        }        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int index = 0;
        int movingPartsIndex = 0;
        onTarget = productClass.triggered;
        newLocation = product.transform.localPosition;
        GetActiveArray();

        foreach (Transform child in boxesArray)
        {
            if (child != null)
            {
                if (Array.IndexOf(activeArray, child) != -1){
                    float newYPosition = child.localPosition.y + actions.ContinuousActions[movingPartsIndex] * MoveSpeed * Time.deltaTime;
                    newYPosition = Mathf.Clamp(newYPosition, 0f, 4.7f);
                    child.localPosition = new Vector3(child.localPosition.x, newYPosition, child.localPosition.z);
                    movingPartsIndex++;
                }
                else
                {
                    int i = index / rows;
                    int j = index % columns;
                    child.transform.localPosition = boxesLoc[i, j];
                }
                index++;
            }
            else{
                Debug.Log("Null child founded!");
            }
        }
        directionPoint = Vector3.Dot(productRigidbody.velocity.normalized, (target.transform.localPosition - product.transform.localPosition).normalized);
        if(directionPoint<0.3&&directionPoint>0){directionPoint*=-1;}
        var heightPoint = Math.Abs(product.transform.localPosition.y-6.8f);
        if (!onTarget){
            float reward = 0.0001f * ((directionPoint*productRigidbody.velocity.magnitude)+0.00001f)*(1f/((heightPoint*targetCloseness())+0.0001f));
            AddReward(reward);            
        }
        if (showUI)
        {
            updateUI();
        }
        oldLocation = product.transform.localPosition;
    }



    public override void OnEpisodeBegin()
    {
        if(!randomTableSize){
            if(CompletedEpisodes%50 == 0){
                table.RecreateMaze();
            }
            table.ObjectPos(table.GetWallDist());     
        }
        else{
            table.ResetEnv();
            rows = table.rows;
            columns = table.columns;
            wallBorders = null;
            boxesArray = new Transform[rows,columns];
            boxesLoc = new Vector3[rows,columns];
            wallBorders = table.getBorders;
            boxesArray = table.getPieces;
            productCollision075 productClass = product.GetComponent<productCollision075>();
            productClass.InitializeProduct(table.wallsArray,target,gameObject);
            for(int i=0; i<rows;i++){
                for(int j=0;j<columns;j++){
                    boxesLoc[i,j]=boxesArray[i,j].transform.localPosition;
                }
            }
        }
        product.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        productRigidbody.velocity = Vector3.zero;
        activeArray = new Transform[size*size-specifiedPoints.Count];
        GetActiveArray();
    }
    
    public void triggerReset(){
        AddReward(-4f);
        EndEpisode();
    }
    
    public void winReset(){
        win++;
        AddReward(2f);
        EndEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (Transform child in activeArray)
        {
            if (child != null)
            {
                sensor.AddObservation(child.localPosition.y);
            }
            else
            {
                sensor.AddObservation(0);
            }
        }
        sensor.AddObservation(product.transform.localPosition);
        sensor.AddObservation(target.transform.localPosition);
        sensor.AddObservation(targetCloseness());
        sensor.AddObservation(productRigidbody.velocity.magnitude);
        RayCollect();
        sensor.AddObservation(observation);
    }

    private void RayCollect(){
        if (observation != null){
            observation.Clear();
        }
        rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput()).RayOutputs;
        for (int i = 0; i < rayOutputs.Length-1; i++)
        {
            var rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
            float rayHitDistance = rayOutputs[i].HitFraction * rayDirection.magnitude;
            observation.Add(rayHitDistance);
        }
    }

    private void updateUI()
    {
        ui.text = "Product States\nBoard Size: "+rows+"x"+columns+"\nDirection: "+directionPoint+"\nSpeed: "+productRigidbody.velocity.magnitude+"\nPosition: "+product.transform.localPosition+"\nDistance to Target: "+targetCloseness()+"\nReward: "+GetCumulativeReward()+"\nAction Count: "+StepCount+"\nGame Count: "+CompletedEpisodes+"\nWin Count: "+win+"\nActive Parts Map: \n"+ActiveMap();
    }
    private string ActiveMap(){
        string arrayString = "";
        int index = 0;
        int[] alpha = new int[specifiedPoints.Count];
        for(int i=0;i<specifiedPoints.Count;i++){
            alpha[i] = specifiedPoints[i].Item1*size+specifiedPoints[i].Item2;
        }
        for (int i = 0; i < size*size; i++)
        {
            if (alpha.Contains(i)){arrayString += "_ ";}
            else{
                if (activeArray[index] != null){arrayString += "O ";}
                else{arrayString += "X ";}
                index++;
            }
            if ((i+1)%size == 0){arrayString += "\n";}
        }
        return arrayString;       
    }

    private void GetActiveArray()
    {
        int[] centerPoint = FindClosestTransform();
        bool isInList;
        int startX = centerPoint[0] - size / 2;
        int startY = centerPoint[1] - size / 2;
        int index = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                isInList = specifiedPoints.Any(tuple => tuple.Item1 == (i,j).Item1 && tuple.Item2 == (i,j).Item2);
                if (!isInList){
                    if(IsIndexValid(startX + i, startY + j) && boxesArray[startX + i, startY + j] != null){
                        activeArray[index] = boxesArray[startX + i, startY + j];
                    }
                    else{
                        activeArray[index] = null;
                    }                    
                index++;
                }
            }
        }
    }
    
    private bool IsIndexValid(int rowIndex, int colIndex)
    {
        return rowIndex >= 0 && rowIndex < rows && colIndex >= 0 && colIndex < columns;
    }    
    
    private int[] FindClosestTransform()
    {
        int[] closestPosition = new int[2];
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                Transform transform = boxesArray[i, j];
                if (transform != null)
                {
                    float distance = GetDistanceToChild(transform);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPosition[0] = i;
                        closestPosition[1] = j;
                    }
                }
            }
        }

        return closestPosition;
    }
    
    private float targetCloseness()
    {
        return Vector3.Distance(product.transform.localPosition, target.transform.localPosition);
    }
    
    private float GetDistanceToChild(Transform child)
    {
        float distance = Vector3.Distance(gameObject.transform.InverseTransformPoint(new Vector3(child.position.x, 0f, child.position.z)), gameObject.transform.InverseTransformPoint(new Vector3(product.transform.position.x, 0f, product.transform.position.z)));
        return distance;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        for (int i = 0; i < size*size-specifiedPoints.Count; i++)
        {
            continuousActions[i] = UnityEngine.Random.Range(-1f, 1f);
        }       
    }

}