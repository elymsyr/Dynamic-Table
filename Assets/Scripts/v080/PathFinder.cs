using System.Collections.Generic;
using UnityEngine;

public class AStar : MonoBehaviour
{
    public Material selectedRoad;
    public Material pathObjects;
    public Material wallDetected;
    private float mean = 48;
    private float scale = 1;
    private int rowColumns => (int)(mean/scale);
    public GameObject cellPrefab;
    public Transform[,] nodes;
    public Transform product;
    public Transform target;
    private Vector3 lastPos = new Vector3(0f,0f,0f);
    public bool pathFinder = true;

    void Start()
    {
        if(pathFinder){initPathFinder();}
    }

    public void initPathFinder(){
        nodes = new Transform[rowColumns, rowColumns];
        createPathObjects();
        initializeNeighborsAll();
    }

    private void Update(){
        if(Vector3.Distance(lastPos,product.localPosition)>1f && pathFinder){
            lastPos = product.localPosition;
            PathFinder(getPoint(product), getPoint(target));
        }
    }

    public void PathFinder(Transform startnode, Transform targetNode){
        resetNodeAll(targetNode);
        List<Transform> path = FindPath(startnode, targetNode);
        if (path != null && path.Count > 0)
        {
            Transform firstNode = path[0];
            colorRoad(path);
        }
        else
        {
            Debug.Log("No path found.");
        }
    }

    public void colorRoad(List<Transform> path){
        foreach(Transform node in nodes){
            if(path.Contains(node)){changeMaterial(node, selectedRoad);}
        }
    }

    private void changeMaterial(Transform node, Material selected){
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
                Vector3 cellPosition = new Vector3(x*scale, 0, y*scale);
                GameObject cellObject = Instantiate(cellPrefab, cellPosition, Quaternion.identity);
                cellObject.transform.parent = transform;
                nodes[x, y] = cellObject.transform;
            }
        }
        transform.localPosition = new Vector3(118f,-326,102.099998f);
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

    private void resetNodeAll(Transform startNode){
        for (int x = 0; x < rowColumns; x++)
        {
            for (int y = 0; y < rowColumns; y++)
            {
                nodes[x,y].GetComponent<Node>().resetNode(startNode);
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
                closedSet.Add(node);
                changeMaterial(node, wallDetected);
            }
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

    private Transform getPoint(Transform point){
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

}
