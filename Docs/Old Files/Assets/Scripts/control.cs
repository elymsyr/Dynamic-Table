using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Barracuda;

public class control : MonoBehaviour
{
    [SerializeField]
    private GameObject parentObject;
    [SerializeField]
    private NNModel nnModel;
    private Model runtimeModel;
    private IWorker worker;
    private string outputLayerName;
    
    
    private void Start()
    {
        runtimeModel = ModelLoader.Load(nnModel);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
        // outputLayerName = runtimeModel.outputs[runtimeModel.outputs.Count - 1];

    }
    private void Update()
    {

    }
}
