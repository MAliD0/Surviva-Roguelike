using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class AlchemyCircleController : MonoBehaviour, IInteractable
{
    [SerializeField] private CircleInstance circleInstance;
    [SerializeField] private AlchemyEngine alchemyEngine;

    [SerializeField] private List<CirclePortInstance> inputPorts = new();
    [SerializeField] private List<CirclePortInstance> outputPorts = new();
    [SerializeField] private CirclePortInstance residuePort;

    [SerializeField] private AlchemyResult lastResult;

    private void Awake()
    {
        circleInstance = GetComponent<CircleInstance>();

        CirclePortInstance[] ports = GetComponentsInChildren<CirclePortInstance>();

        inputPorts = ports
            .Where(p => p.portType == CirclePortType.ItemInput)
            .ToList();

        outputPorts = ports
            .Where(p => p.portType == CirclePortType.ItemOutput)
            .ToList();

        residuePort = ports
            .FirstOrDefault(p => p.portType == CirclePortType.ResidueOutput);
    }

    void Start()
    {
        alchemyEngine = AlchemyEngine.Instance;
    }
    
    [Button]
    public void ExecuteCircle()
    {
        List<ItemSlot> inputItems = new List<ItemSlot>();

        foreach (var item in inputPorts)
        {
            inputItems.Add(item.currentItem);
        }

        AlchemyRequest alchemyRequest = new AlchemyRequest();

        alchemyRequest.inputItems = inputItems;
        alchemyRequest.processType = AlchemyProcessType.Transmute;
        alchemyRequest.circle = circleInstance;

        alchemyEngine.Execute(alchemyRequest);
    }

    public void OnInteract(GameObject interactor, ulong interacterId)
    {
        ExecuteCircle();
    }
}
