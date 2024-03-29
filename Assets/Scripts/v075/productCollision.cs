using UnityEngine;

public class productCollision075 : MonoBehaviour
{
    private GameObject[] walls;
    // private rayRotation rays;
    private GameObject target;
    private GameObject receiverObject;
    public bool triggered = false;

    public void InitializeProduct(GameObject[] setWalls,GameObject setTarget, GameObject receiver){
        walls = setWalls;
        target = setTarget;
        receiverObject = receiver;
        // rays = transform.GetChild(0).GetComponent<rayRotation>();
        // Debug.Log(rays.name);
        // rays.SendMessage("GetInfos");
    } 

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == target)
        {
            receiverObject.SendMessage("winReset");
            triggered = true;
        }
        foreach(GameObject wall in walls){
            if (other.gameObject == wall)
            {
                receiverObject.SendMessage("triggerReset");
            }
        }
    }
    // private void OnTriggerStay(Collider other){
    //     if(other.gameObject == target){
    //         receiverObject.SendMessage("StayReward");
    //     }
    // }

    private void OnTriggerExit(Collider other){
        if(other.gameObject == target){
            triggered = false;
        }
    }    
}
