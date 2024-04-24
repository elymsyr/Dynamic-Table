using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public float gCost = 999999f;
    public float hCost = 999999f;
    public bool wallFlag = false;
    public LayerMask layerMask;
    public float fCost => gCost + hCost;
    public List<Transform> neighbors;
    public Transform parent;

    public void CheckCollide(){
        Vector3[] directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            Vector3.forward + Vector3.right,
            Vector3.forward + Vector3.left,
            Vector3.back + Vector3.right,
            Vector3.back + Vector3.left
        };
        foreach (Vector3 direction in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, 3f, layerMask))
            {
                wallFlag = true;
                break;
            }
        }
    }

    public void resetNode(Transform target){
        wallFlag = false;
        CheckCollide();
        if(!wallFlag){
            hCost = Vector3.Distance(transform.localPosition, target.localPosition);
        }
        else{hCost = 999999f;}
        gCost = 999999f;
        parent = null;
    }

    public void initializeNeighbors(List<Transform> newNeighbors){
        neighbors = newNeighbors;
    }

    public float setgCost(Transform neighbor){
        gCost = Vector3.Distance(transform.localPosition, neighbor.localPosition);
        return Vector3.Distance(transform.localPosition, neighbor.localPosition);
    }

}
