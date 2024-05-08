using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents;
using TMPro;
using Unity.MLAgents.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.Collections.ObjectModel;

public class dynamicTable090 : Agent
{
    [Header("UI")]
    [SerializeField] private bool showUI = false;
    [SerializeField] private GameObject text;
    [Header("Maze")]
    [SerializeField] private bool maze = false;
    [SerializeField] [Range(0,9)]private int mazeNumber = 0;
    [SerializeField] private GameObject mazesParent;
    [SerializeField] [Range(0f,4f)] private float distanceMul = 1;
    private Vector3 mazeLocationSet = new Vector3(158f,6f,1.25f);
    private List<Transform> mazes;
    private GameObject randomMaze => mazes[UnityEngine.Random.Range(0, mazes.Count)].gameObject;
    private GameObject selectedMaze;
    [Header("Set")]
    [SerializeField] private GameObject pathfinder;
    [SerializeField] private GameObject product;
    [SerializeField] private GameObject target;
    [Range(0f,15f)] public float MoveSpeed = 12f;

    private int winState = 0;
    private Queue<int> gameStates = new Queue<int>();
    private AStar090 AStar;
    private int RowsNColumns = 22;
    private float closeness = 0;
    private float scale = 4f; 
    private TextMeshPro ui;
    private float directionPoint = 0;
    private int rows = 22;
    private int columns = 22;
    private Transform[,] boxesArray;
    private Vector3[,] boxesLoc;
    private float[] wallBorders = {14.2f,-15.2f,14.2f,-15.2f};
    public float[] getBorders => wallBorders;
    private int size = 8;
    private Transform[] activeArray;
    private Rigidbody productRigidbody;
    private List<Tuple<int, int>> specifiedPoints = new List<Tuple<int, int>>(){new Tuple<int, int>(0, 0),new Tuple<int, int>(0, 1),new Tuple<int, int>(1, 0),new Tuple<int, int>(2, 0),new Tuple<int, int>(0, 2),new Tuple<int, int>(0, 5),new Tuple<int, int>(0, 6),new Tuple<int, int>(0, 7),new Tuple<int, int>(1, 7),new Tuple<int, int>(2, 7),new Tuple<int, int>(5, 0),new Tuple<int, int>(6, 0),new Tuple<int, int>(7, 0),new Tuple<int, int>(7, 1),new Tuple<int, int>(7, 2),new Tuple<int, int>(7, 5),new Tuple<int, int>(7, 6),new Tuple<int, int>(7, 7),new Tuple<int, int>(6, 7),new Tuple<int, int>(5, 7),};
    private productCollision090 productClass;
    private StatsRecorder recorder;
    private int lastStep = 0;
    private float heighpoint;
    private List<float> observation; // RAY
    private RayPerceptionOutput.RayOutput[] rayOutputs; // RAY
    private RayPerceptionSensorComponent3D rayPerceptionSensor; // RAY
    private List<Transform> path;


    void Awake()
    {
        mazes = new List<Transform>();
        if (pathfinder != null){AStar = pathfinder.GetComponent<AStar090>();}
        path = new List<Transform>();
        if(AStar.pathFinder){AStar.initPathFinder();}

        for (int i = 0; i < mazesParent.transform.childCount; i++)
        {
            Transform childMaze = mazesParent.transform.GetChild(i);
            mazes.Add(childMaze);
        }

        rows = RowsNColumns;
        columns = RowsNColumns;
        productClass = product.GetComponent<productCollision090>();
        observation = new List<float>(10); // RAY
        Transform ray = product.transform.GetChild(0); // RAY
        rayPerceptionSensor = ray.GetComponent<RayPerceptionSensorComponent3D>(); // RAY    
        recorder = Academy.Instance.StatsRecorder;

        if (text != null)
        {
            ui = text.GetComponent<TextMeshPro>();
        }

        productRigidbody = product.GetComponent<Rigidbody>();

        int childIndex = 0;
        int rowIndex = 0;
        int columnIndex = 0;
        boxesArray = new Transform[rows, columns];
        boxesLoc = new Vector3[rows, columns];
        Transform pieces = transform.Find("Pieces");
        for (int i = 0; i < rows*columns; i++)
        {
            Transform child = pieces.GetChild(childIndex);
            boxesArray[rowIndex, columnIndex] = child;
            boxesLoc[rowIndex, columnIndex] = child.localPosition;
            columnIndex++;
            if (columnIndex >= columns)
            {
                columnIndex = 0;
                rowIndex++;
            }
            childIndex++;
        }
    }

    void Update(){
        if(AStar.pathFinder && Vector3.Distance(AStar.lastPos,product.transform.localPosition)>1.5f){ 
            AStar.lastPos = product.transform.localPosition;
            path = AStar.PathFinder(AStar.getPoint(product.transform), AStar.getPoint(target.transform), path);
        }        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if(product.transform.localPosition.y < 0){EndEpisode();}
        int index = 0;
        int movingPartsIndex = 0;
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
        if(!productClass.triggered){
            directionPoint = Vector3.Dot(productRigidbody.velocity.normalized, (target.transform.localPosition - product.transform.localPosition).normalized);
            float heightPoint = product.transform.localPosition.y;
            float closeness = targetCloseness();
            float speed = productRigidbody.velocity.magnitude;
            if(directionPoint<0.6f && directionPoint>0 && !maze){directionPoint*=-1;}
            if(speed < 0.1f){speed = 0.1f;}
            if(closeness<0.1){closeness = 0.1f;}
            float reward_increase = directionPoint * speed * 0.0002f;
            float reward_decrease = (float)Math.Pow(closeness, 0.1f);
            float reward = reward_increase / reward_decrease;
            if(directionPoint>=0){
                reward += heightPoint*0.0001f*0.2f;
            }
            AddReward(reward);
        }
        if (showUI)
        {
            updateUI();
        }
        lastStep = StepCount;
    }   
    
    public override void OnEpisodeBegin()
    {
        if(selectedMaze!=null){Destroy(selectedMaze);}
        if(mazeNumber >= 1 && mazeNumber <= 8){selectedMaze = Instantiate(mazes[mazeNumber].gameObject, mazeLocationSet, Quaternion.identity);}
        else if(mazeNumber == 9){Instantiate(randomMaze, mazeLocationSet, Quaternion.identity);}

        gameStates.Enqueue(winState);
        if(gameStates.Count > 100){
            gameStates.Dequeue();
        }
        winState = 0;
        recorder.Add("Custom/Win Percentage of Last 100 Episodes",CalculatePercentageOfOnes(),StatAggregationMethod.Average);
        recorder.Add("Custom/Completed Episodes",CompletedEpisodes,StatAggregationMethod.Average);
        recorder.Add("Custom/Avg Step",lastStep,StatAggregationMethod.Average);

        product.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        productRigidbody.velocity = Vector3.zero;
        activeArray = new Transform[size*size-specifiedPoints.Count];
        ObjectPos();

        AStar.resetNodeAll(AStar.getPoint(target.transform), distanceMul);
    }

    private float CalculatePercentageOfOnes()
    {   if(gameStates.Count > 0){
            int totalOnes = 0;
            foreach (int item in gameStates)
            {
                if (item == 1)
                {
                    totalOnes++;
                }
            }
            return ((float)totalOnes / gameStates.Count) * 100f;
        }
        else{return 0;}
    }     

    public void triggerReset(){
        AddReward(-4f);
        EndEpisode();
    }
    
    public void winReset(){
        winState = 1;
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

        if(!maze){
            sensor.AddObservation(target.transform.localPosition);
        }
        else{
            if(path != null && path.Count > 0){
                sensor.AddObservation(gameObject.transform.InverseTransformPoint(path[0].position));
                // AStar.changeMaterial(path[0], focusTarget);
            }
            else{sensor.AddObservation(target.transform.localPosition);}
        }

        sensor.AddObservation(targetCloseness());
        sensor.AddObservation(productRigidbody.velocity.magnitude);
        RayCollect(); // RAY
        sensor.AddObservation(observation); // RAY
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

    private Vector3 randomPos(){
        return new Vector3(UnityEngine.Random.Range(wallBorders[0]-scale, wallBorders[1]+scale), UnityEngine.Random.Range(6f,7f), UnityEngine.Random.Range(wallBorders[2]-scale, wallBorders[3]+scale));
    }     

    public void ObjectPos(){
        Vector3 target_start;
        Vector3 product_start;
        do{
            target_start = randomPos();
            product_start = randomPos();
        }while(Vector3.Distance(target_start, product_start) < 6f && AStar.checkPoint(product.transform) && AStar.checkPoint(target.transform));
        target.transform.localPosition = target_start;
        product.transform.localPosition = product_start;
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        for (int i = 0; i < size*size-specifiedPoints.Count; i++)
        {
            continuousActions[i] = UnityEngine.Random.Range(-1f, 1f);
        }       
    }

    private void updateUI()
    {
        ui.text = "Product States\nTarget Location: "+TargetLocation()+"\nAre we winning? "+CalculatePercentageOfOnes()+"\nBoard Size: "+rows+"x"+columns+"\nDirection: "+directionPoint+"\nSpeed: "+productRigidbody.velocity.magnitude+"\nPosition: "+product.transform.localPosition+"\nHeighpoint: "+heighpoint+"\nDistance to Target: "+closeness+"\nReward: "+GetCumulativeReward()+"\nAction Count: "+StepCount+"\nGame Count: "+CompletedEpisodes+"\nActive Parts Map: \n"+ActiveMap();
    }

    private string TargetLocation(){
        ReadOnlyCollection<float> observation = GetObservations();
        if(observation.Count == 0){
            return "         ";
        }
        else{
            string str = observation.Count+": ";
            str +=observation[47] + " | ";
            str +=observation[48] + " | ";
            str +=observation[49];    
            return str;
        }
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
            if (alpha.Contains(i)){arrayString += " _ ";}
            else{
                if (activeArray[index] != null){arrayString += (int)activeArray[index].transform.localPosition.y+" ";}
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

}