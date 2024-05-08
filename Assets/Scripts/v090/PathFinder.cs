using System.Collections.Generic;
using UnityEngine;

public class AStar090 : MonoBehaviour
{
    public Material selectedRoad;
    public Material pathObjects;
    public Material wallDetected;
    public Material test;
    public float mean = 26;
    public float scale = 0.5f;
    private int rowColumns => (int)(mean/scale);
    public Transform[,] nodes;
    public Transform product;
    public Transform target;
    public Vector3 lastPos = new Vector3(0f,0f,0f);
    public bool pathFinder = true;

    public void initPathFinder(){
        nodes = new Transform[rowColumns, rowColumns];
        createPathObjects();
        initializeNeighborsAll();
    }

    public List<Transform> PathFinder(Transform startnode, Transform targetNode, List<Transform> pathNodes){
        // resetNodeAll(targetNode);
        List<Transform> first_path = new List<Transform>();
        first_path = FindPath(startnode, targetNode);
        if (first_path != null && first_path.Count > 0)
        {
            pathNodes.Clear();
            for(int i = 9;i<first_path.Count-5;i++){
                if(i%4 == 0){
                    pathNodes.Add(first_path[i]);
                }
            }
            colorRoad(pathNodes);
        }
        return pathNodes;
    }

    public void colorRoad(List<Transform> path){
        foreach(Transform node in nodes){
            if(path.Contains(node)){changeMaterial(node, selectedRoad);}
        }
    }

    public void changeMaterial(Transform node, Material selected){
        Renderer renderer = node.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = selected;
        }
    }

    private void createPathObjects(){
        for (int x = 0; x < rowColumns; x++)
        {
            for (int y = 0; y < rowColumns; y++)
            {
                nodes[x, y] = transform.GetChild(x * rowColumns + y);
            }
        }
    }

    private List<Transform> GetSurroundingTransforms(int x, int y)
    {
        List<Transform> surroundingTransforms = new List<Transform>();
        int[] offsetX = { -1,  0,  1, -1, 1, -1, 0, 1 };
        int[] offsetY = { -1, -1, -1,  0, 0,  1, 1, 1 };
        for (int i = 0; i < offsetX.Length; i++)
        {
            int newX = x + offsetX[i];
            int newY = y + offsetY[i];

            if (newX >= 0 && newX < rowColumns && newY >= 0 && newY < rowColumns)
            {
                surroundingTransforms.Add(nodes[newX, newY]);
            }
        }

        return surroundingTransforms;
    }

    private void initializeNeighborsAll(){
        for (int x = 0; x < rowColumns; x++)
        {
            for (int y = 0; y < rowColumns; y++)
            {
                nodes[x,y].GetComponent<Node>().initializeNeighbors(GetSurroundingTransforms(x,y));
            }
        }
    }

    public void resetNodeAll(Transform startNode, float distanceMul=1.0f){
        for (int x = 0; x < rowColumns; x++)
        {
            for (int y = 0; y < rowColumns; y++)
            {
                nodes[x,y].GetComponent<Node>().resetNode(startNode, distanceMul);
                changeMaterial(nodes[x,y], pathObjects);
            }
        }
    }

    private List<Transform> FindPath(Transform start, Transform goal)
    {
        List<Transform> openSet = new List<Transform>();
        HashSet<Transform> closedSet = new HashSet<Transform>();
        openSet.Add(start);
        foreach(Transform node in nodes){
            if(node.GetComponent<Node>().wallFlag){
                changeMaterial(node, wallDetected);
            }
            else{changeMaterial(node, pathObjects);}
        }

        while (openSet.Count > 0)
        {
            Transform currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].GetComponent<Node>().fCost < currentNode.GetComponent<Node>().fCost || (openSet[i].GetComponent<Node>().fCost == currentNode.GetComponent<Node>().fCost && openSet[i].GetComponent<Node>().hCost < currentNode.GetComponent<Node>().hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == goal)
            {
                return RetracePath(start, goal);
            }

            foreach (Transform neighbor in currentNode.GetComponent<Node>().neighbors)
            {
                if(neighbor != null){
                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    float newMovementCostToNeighbor = currentNode.GetComponent<Node>().setgCost(neighbor) + GetDistance(currentNode, neighbor); 

                    if (newMovementCostToNeighbor < neighbor.GetComponent<Node>().gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GetComponent<Node>().gCost = newMovementCostToNeighbor;
                        neighbor.GetComponent<Node>().parent = currentNode;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
        }

        return null;
    }

    private List<Transform> RetracePath(Transform startNode, Transform endNode)
    {
        List<Transform> path = new List<Transform>();
        Transform currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.GetComponent<Node>().parent;
        }
        path.Reverse();
        return path;
    }

    private float GetDistance(Transform nodeA, Transform nodeB)
    {
        return Vector3.Distance(gameObject.transform.InverseTransformPoint(nodeA.position), gameObject.transform.InverseTransformPoint(nodeB.position));
    }  

    public Transform getPoint(Transform point){
        float distance = 1000f;
        Transform pointNode = null;
        foreach(Transform node in nodes){
            if(!node.GetComponent<Node>().wallFlag){
                float newDist = GetDistance(node, point);
                if(newDist<distance){
                    pointNode = node;
                    distance = newDist;
                }
            }
        }
        return pointNode;
    }

    public bool checkPoint(Transform point){
        int n = 10;
        // Create lists to hold the closest objects and their distances
        List<Transform> closestNodes = new List<Transform>();
        List<float> closestDistances = new List<float>();

        // Initialize the lists with dummy values
        for (int i = 0; i < n; i++)
        {
            closestNodes.Add(null);
            closestDistances.Add(float.MaxValue);
        }

        foreach(Transform node in nodes)
        {
            float newDist = GetDistance(node, point);
            // Check if the new distance is smaller than any of the distances in the list
            for (int i = 0; i < n; i++)
            {
                if (newDist < closestDistances[i])
                {
                    // Shift elements to the right to make space for the new distance
                    for (int j = n - 1; j > i; j--)
                    {
                        closestDistances[j] = closestDistances[j - 1];
                        closestNodes[j] = closestNodes[j - 1];
                    }
                    // Insert the new distance and node at the appropriate position
                    closestDistances[i] = newDist;
                    closestNodes[i] = node;
                    break; // Stop searching for the next position
                }
            }
        }
        foreach(Transform node in closestNodes){
            changeMaterial(node, test);
        }

        foreach(Transform node in closestNodes){
            if(node.GetComponent<Node>().wallFlag){
                return false;
            }
        }
        return true;
    }    

}
