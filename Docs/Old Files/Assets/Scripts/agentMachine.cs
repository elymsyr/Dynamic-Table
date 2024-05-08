using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;

public class ChildObjectManager : Agent
{
    public bool switchBehavior = true;
    private GameObject parentObject;
    // private string path = Application.dataPath + "/trainLog/Log.txt";
    private int rows = 20;
    private int columns = 20;
    private float minY = 1.75f;
    private float maxY = 3.5f;
    // private float moveDist = 5f;
    public float moveSpeed = 0.7f;
    public int distance_lim = 9;
    private Transform[,] childArray;
    // private float[,] movementObserveArray;
    // private GameObject wall;

    // private float angleTolerance = 40f;
    private Rigidbody productRigidbody;
    private TextMeshPro ui;
    private Vector3 savedProductLoc;
    private Vector3 transformLoc;
    public int actionLimit = 1700;
    private int actionCount = 0;
    private Vector3[,] tableLoc;
    private int gameCount = 0;
    private int win = 0;
    private Queue<float> last_rewards;
    private float lastReward = 0;
    private float legalY;
    private float startDistance;
    
    private int moving_parts = 0;
    public int avr_rewards = 1000;
    [SerializeField]
    private GameObject env;
    [SerializeField]
    private GameObject product;
    [SerializeField]
    private GameObject target;
    [SerializeField]
    private GameObject uiGameObject;
    // [SerializeField]
    // [Range(0.0f, 2.0f)]
    // private float multiplier = 1.5f;
    // private IWorker worker;

    private void Awake()
    {
        // Time.timeScale = Time.timeScale * multiplier;
        // Time.fixedDeltaTime = Time.fixedDeltaTime * multiplier;

        // using (StreamWriter sw = new StreamWriter(path))
        // {
        //     sw.WriteLine("\nTraining Started...");
        // }
        last_rewards = new Queue<float>(avr_rewards);
        transformLoc = transform.localPosition;
        // product = GameObject.FindWithTag("Product");
        // env = GameObject.FindWithTag("Environment");
        // wall = GameObject.FindWithTag("Wall");
        savedProductLoc = product.transform.localPosition;
        // GameObject uiGameObject  = GameObject.FindWithTag("GeneralText");
        productRigidbody = product.GetComponent<Rigidbody>();
        if (uiGameObject != null){
            ui = uiGameObject.GetComponent<TextMeshPro>();
        }
        // target = GameObject.FindWithTag("Target");
        legalY = target.transform.localPosition.y;
        parentObject = transform.gameObject;
        if (parentObject != null)
        {
            GetChildObjects();
        }
        else
        {
            Debug.LogError("Parent object not assigned!");
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

    public void triggerReset(){
        lastReward = -15;
        AddReward(-15f);
        AddValue(-15f);
        EndEpisode();
    }

    public void winReset(){
            float reward = rewardCalculate(0);
            AddValue(reward);
            AddReward(reward);
            EndEpisode();
    }

    public float CurrentAverage()
    {
        if (last_rewards.Count == 0)
        {
            return 0f;
        }

        return last_rewards.Average();
    }

    public void AddValue(float newValue)
    {
        if (last_rewards.Count == avr_rewards)
        {
            last_rewards.Dequeue();
        }

        last_rewards.Enqueue(newValue);
    }

    private float rewardCalculate(float point)
    {
        float reward;
        if (point == 0){
            reward = (actionLimit-actionCount)/(startDistance*5)+10f;
            win++;
        }
        else{
            reward = ((startDistance-targetCloseness())*10f/startDistance) - 10f;
        }
        lastReward = reward;
        return reward;
    }

    private void updateUI()
    {
        ui.text = "Product States\nPosition: "+getProductPos()+"\nDistance to Target: "+targetCloseness()+"\nAction Count: "+actionCount+"\nGame Count: "+gameCount+"\nWin Count: "+win+"\nAvg of Last "+avr_rewards+" Rewards: "+CurrentAverage()+"\nLast Reward: "+lastReward+"\nMoving Parts: "+moving_parts; //+"\nAngle Correctness: "+getProductRot() // \nSpeed: "+getProductSpeed()+"
    }

    // private void MoveChildren()
    // {
    //     // movementObserveArray = new float[rows, columns];
    //     int index = 0;
    //     foreach (Transform child in childArray)
    //     {
    //         if (child != null)
    //         {
    //             if (GetDistanceToChild(child))
    //             {
    //                 float randomDirection = UnityEngine.Random.Range(-moveDist, moveDist);
    //                 float newYPosition = child.localPosition.y + randomDirection * moveSpeed * Time.deltaTime;
    //                 newYPosition = Mathf.Clamp(newYPosition, minY, maxY);
    //                 child.localPosition = new Vector3(child.localPosition.x, newYPosition, child.localPosition.z);
    //             }
    //             int i = index / rows;
    //             int j = index % columns;
    //             // movementObserveArray[i, j] = child.localPosition.y;
    //             index++;
    //         }
    //     }
    // }
    // private bool getProductRot()
    // {
    //     Quaternion currentRotation = product.transform.localRotation;
    //     Quaternion targetRotation = Quaternion.Euler(0f, 0f, 0f);
    //     float angleDifference = Quaternion.Angle(currentRotation, targetRotation);
    //     return angleDifference <= angleTolerance;
    // }
    // private float getProductSpeed()
    // {
    //     return productRigidbody.velocity.magnitude;
    // }
    private Vector3 getProductPos()
    {
        return product.transform.localPosition;
    }
    private float targetCloseness()
    {
        float distance = Vector3.Distance(product.transform.localPosition, target.transform.localPosition);
        if (distance < 1)
        {
            return 0;
        }
        return distance;
    }
    private bool GetDistanceToChild(Transform child)
    {
        float distance = Vector3.Distance(env.transform.InverseTransformPoint(child.position), env.transform.InverseTransformPoint(product.transform.position));
        return distance < distance_lim;
    }

    private Vector3 targetRandomPos(){
            int i = UnityEngine.Random.Range(0, 2);
            float x;
            float z;
            if(i == 0){
                x = UnityEngine.Random.Range(-39f, -20f);
                z = UnityEngine.Random.Range(6.3f, 0.85f);
            }
            else{
                x = UnityEngine.Random.Range(-25f, -20f);
                z = UnityEngine.Random.Range(20f, 0.85f);
            }

            return new Vector3(x, legalY, z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        moving_parts = 0;
        if (actionLimit < actionCount) //  || getProductRot() != true
        {
            float reward = rewardCalculate(1);
            AddValue(reward);
            AddReward(reward);
            EndEpisode();
        }
        int index = 0;
        actionCount++;
        foreach (Transform child in childArray)
        {
            if (child != null)
            {
                if (switchBehavior || GetDistanceToChild(child)){
                    float randomDirection = actions.ContinuousActions[index]*10;
                    float newYPosition = child.localPosition.y + randomDirection * moveSpeed * Time.deltaTime;
                    newYPosition = Mathf.Clamp(newYPosition, minY, maxY);
                    child.localPosition = new Vector3(child.localPosition.x, newYPosition, child.localPosition.z);
                    moving_parts++;
                }
                else
                {
                    int i = index / rows;
                    int j = index % columns;
                    child.transform.localPosition = tableLoc[i, j];
                }

                index++;
            }
            else{
                Debug.Log("Null child founded!");
            }
        }
        if (ui != null){
            updateUI();
        }
    }

    public override void OnEpisodeBegin()
    {
        productRigidbody.velocity = Vector3.zero;
        transform.localPosition = transformLoc;
        product.transform.localPosition = savedProductLoc;
        target.transform.localPosition = targetRandomPos();
        startDistance = targetCloseness();
        actionCount = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                childArray[i,j].transform.localPosition = tableLoc[i,j];
            }
        }
        gameCount++;
        if (gameCount%avr_rewards==0){
            Debug.Log("\nAvg Rewards ("+avr_rewards+"): "+CurrentAverage()+" -> Game Count: "+gameCount+" -> Win Count: "+win+" -> Last Reward: "+lastReward);
            // using (StreamWriter sw = new StreamWriter(path))
            // {
            //     sw.Write("\nGame Count: "+gameCount+"\nWin Count: "+win+"\nAvg of Last "+avr_rewards+" Rewards: "+CurrentAverage()+"\nLast Reward: "+lastReward);
            // }
        }
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

        sensor.AddObservation(getProductPos());
        // sensor.AddObservation(getProductSpeed());
        sensor.AddObservation(targetCloseness());
        // sensor.AddObservation(getProductRot());
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        for (int i = 0; i < columns*rows; i++)
        {
            continuousActions[i] = UnityEngine.Random.Range(-1f, 1f);
            // continuousActions[i] = Input.GetAxisRaw("Vertical");
        }
    }

}
