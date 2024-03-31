using UnityEngine;

public class productCollision075 : MonoBehaviour
{
    private GameObject[] walls;
    private GameObject target;
    private GameObject receiverObject;
    public bool triggered = false;

    public void InitializeProduct(GameObject[] setWalls,GameObject setTarget, GameObject receiver){
        walls = setWalls;
        target = setTarget;
        receiverObject = receiver;
    } 

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == target)
        {
            receiverObject.SendMessage("winReset");
            triggered = true;
        }
        if (other.gameObject.CompareTag("Wall"))
        {
            receiverObject.SendMessage("triggerReset");
        }
    }

    private void OnTriggerExit(Collider other){
        if(other.gameObject == target){
            triggered = false;
        }
    }    
}
