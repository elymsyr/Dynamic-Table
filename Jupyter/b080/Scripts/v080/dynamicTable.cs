using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents;
using TMPro;
using Unity.MLAgents.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class dynamicTable08 : Agent
{
    private int win = 0;
    private int winState = 0;
    private Queue<int> gameStates = new Queue<int>();
    [SerializeField] private bool showUI = false;
    [SerializeField] private int RowsNColumns = 22;
    [SerializeField] private GameObject text;
    [SerializeField] private GameObject product;
    [SerializeField] private GameObject target;
    [SerializeField] private float freq = 250;
    private float closeness = 0;
    private float scale = 4f; 
    private TextMeshPro ui;
    private float directionPoint = 0;
    private int rows = 40;
    private int columns = 40;
    private Transform[,] boxesArray;
    private Vector3[,] boxesLoc;
    // private float[] wallBorders = {-26,25,25,-26};
    private float[] wallBorders = {-15.2f,14.2f,14.2f,-15.2f};
    private int size = 8;
    [Range(0f,15f)] public float MoveSpeed = 12f;
    private Transform[] activeArray;
    private Rigidbody productRigidbody;
    private List<Tuple<int, int>> specifiedPoints = new List<Tuple<int, int>>(){new Tuple<int, int>(0, 0),new Tuple<int, int>(0, 1),new Tuple<int, int>(1, 0),new Tuple<int, int>(2, 0),new Tuple<int, int>(0, 2),new Tuple<int, int>(0, 5),new Tuple<int, int>(0, 6),new Tuple<int, int>(0, 7),new Tuple<int, int>(1, 7),new Tuple<int, int>(2, 7),new Tuple<int, int>(5, 0),new Tuple<int, int>(6, 0),new Tuple<int, int>(7, 0),new Tuple<int, int>(7, 1),new Tuple<int, int>(7, 2),new Tuple<int, int>(7, 5),new Tuple<int, int>(7, 6),new Tuple<int, int>(7, 7),new Tuple<int, int>(6, 7),new Tuple<int, int>(5, 7),};
    private productCollision08 productClass;
    private StatsRecorder recorder;
    private int lastStep = 0;
    private float heighpoint;

    void Awake()
    {
        rows = RowsNColumns;
        columns = RowsNColumns;
        productClass = product.GetComponent<productCollision08>();
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
        if (!productClass.triggered){
            directionPoint = Vector3.Dot(productRigidbody.velocity.normalized, (target.transform.localPosition - product.transform.localPosition).normalized);
            closeness = shrunk(targetCloseness(), min:0.001f, max:40, newMin:1, newMax:10);
            heighpoint = Math.Abs(product.transform.localPosition.y - 6.1f);
            if(heighpoint<0.1f){heighpoint=0.1f;}
            float speed = productRigidbody.velocity.magnitude;
            if(directionPoint<0.6/freq && directionPoint>0){directionPoint*=-1;}
            if(speed < 0.1f){speed = 0.1f;}
            float reward = ((directionPoint*speed*speed)-heighpoint)/closeness;
            AddReward(shrunk(reward, reward_state:true));
        }
        if (showUI)
        {
            updateUI();
        }
        lastStep = StepCount;
    }

    private float shrunk(float reward, float min = -100 , float max = 100, float newMin = -0.1f, float newMax = 0.1f, bool reward_state = false){
        if(reward_state==true){
            if(reward>0){newMin=0; min = 0;}
            if(reward<0){newMax=0; max = 0;}
        }
        return newMin + ((newMax-newMin)*(reward-min)/(max-min));
    }    
    
    public override void OnEpisodeBegin()
    {
        gameStates.Enqueue(winState);
        if(gameStates.Count > 500){
            gameStates.Dequeue();
        }
        winState = 0;
        MaxStep = (int)(200f+freq/2);
        if (freq > 500){
            freq = 500;
        }
        else if(freq < 1){
            freq = 1;
        }
        recorder.Add("Custom/Win Percentage of Last 500 Episodes",CalculatePercentageOfOnes(),StatAggregationMethod.Average);
        recorder.Add("Custom/Completed Episodes",CompletedEpisodes,StatAggregationMethod.Average);
        recorder.Add("Custom/Avg Step",lastStep,StatAggregationMethod.Average);
        recorder.Add("Custom/Freq",freq,StatAggregationMethod.Average);
        product.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        productRigidbody.velocity = Vector3.zero;
        activeArray = new Transform[size*size-specifiedPoints.Count];
        ObjectPos();
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
        freq += 0.7f;
        AddReward(-2f);
        EndEpisode();
    }
    
    public void winReset(){
        win++;
        winState = 1;
        freq -= 500/freq;
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
    }

    private Vector3 randomPos(){
        return new Vector3(UnityEngine.Random.Range(wallBorders[0]-(scale/2 + 0.1f), wallBorders[1]+(scale/2 + 0.1f)), UnityEngine.Random.Range(6f,7f), UnityEngine.Random.Range(wallBorders[2]-(scale/2 + 0.1f), wallBorders[3]+(scale/2 + 0.1f)));
    }     

    public void ObjectPos(){
        Vector3 target_start;
        Vector3 product_start;
        do{
            target_start = randomPos();
            product_start = randomPos();
        }while(Vector3.Distance(target_start, product_start) < 6f);
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
        ui.text = "Product States\nAre we winning? "+CalculatePercentageOfOnes()+"\nFrequency: "+freq+" ("+(int)freq+") "+"\nBoard Size: "+rows+"x"+columns+"\nDirection: "+directionPoint+"\nSpeed: "+productRigidbody.velocity.magnitude+"\nPosition: "+product.transform.localPosition+"\nHeighpoint: "+heighpoint+"\nDistance to Target: "+closeness+"\nReward: "+GetCumulativeReward()+"\nAction Count: "+StepCount+"\nGame Count: "+CompletedEpisodes+"\nWin Count: "+win+"\nActive Parts Map: \n"+ActiveMap();
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