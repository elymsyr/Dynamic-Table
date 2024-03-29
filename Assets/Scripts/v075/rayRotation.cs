using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class rayRotation : MonoBehaviour
{
    // private Transform parent;
    // dynamicTable075 table;
    // private Vector3 direction;
    RayPerceptionOutput.RayOutput[] rayOutputs;
    public List<float> observation;

    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    private Quaternion _initialRotation = Quaternion.Euler(0f,0f,0f);
    
    // private void GetInfos(){
    //     _initialRotation = transform.rotation;
    //     Transform parent = transform.parent.parent;
    //     table = parent.GetComponent<dynamicTable075>();
    //     direction = table.direction;
    // }

    private void Awake(){
        rayPerceptionSensor = transform.GetComponent<RayPerceptionSensorComponent3D>();
    }
    
    private void Update(){
        observation.Clear();
        transform.rotation = _initialRotation;
        rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput()).RayOutputs;
        Debug.Log(rayOutputs.Length);
        for (int i = 0; i < rayOutputs.Length; i++)
        {
            GameObject goHit = rayOutputs[i].HitGameObject;
            if (goHit != null)
            {
                var rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
                float rayHitDistance = rayOutputs[i].HitFraction * rayDirection.magnitude;
                observation.Add(rayHitDistance);
            }
        }        
    //     if(table!=null){
    //         direction = table.direction;
    //         if (direction != Vector3.zero)
    //         {
    //             // Calculate the rotation to look at the direction
    //             Quaternion targetRotation = Quaternion.LookRotation(direction);

    //             // Apply the rotation
    //             transform.rotation = targetRotation;
    //         }               
    //     }
      
    }

}
