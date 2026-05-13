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
        if (residuePort != null && !residuePort.IsEmpty)
            return;

        foreach (var port in outputPorts)
        {
            if (port != null && !port.IsEmpty)
                return;
        }

        List<ItemSlot> inputItems = new List<ItemSlot>();

        foreach (var port in inputPorts)
        {
            if (port != null && port.currentItem != null && !port.currentItem.IsEmpty())
                inputItems.Add(new ItemSlot(port.currentItem));
        }

        if (inputItems.Count == 0)
        {
            Debug.LogWarning("[AlchemyCircle] No input items.");
            return;
        }

        AlchemyRequest alchemyRequest = new AlchemyRequest();
        alchemyRequest.inputItems = inputItems;
        alchemyRequest.processType = AlchemyProcessType.Transmute;
        alchemyRequest.circle = circleInstance;

        lastResult = alchemyEngine.Execute(alchemyRequest);

        if (lastResult == null || !lastResult.success || lastResult.outputItem == null)
        {
            Debug.LogWarning($"[AlchemyCircle] Failed: {lastResult?.failureReason}");
            return;
        }

        if (outputPorts.Count <= 0 || outputPorts[0] == null)
        {
            Debug.LogWarning("[AlchemyCircle] No output port.");
            return;
        }

        outputPorts[0].AddItem(lastResult.outputItem, 1);

        if (lastResult.residueProfile != null && !lastResult.residueProfile.IsEmpty())
        {
            ItemSlot residueSlot = CreateResidue(lastResult.residueProfile);

            if (residuePort != null)
                residuePort.SetItem(residueSlot);
        }

        ClearInputPorts();
    }

    private ItemSlot CreateResidue(AspectProfile customAspectProfile)
    {
        ResidueItemData residueItemData = AlchemyItemDatabase.Instance.GetResidueItem();

        AlchemyItemStackData stackData = new AlchemyItemStackData
        {
            qualityTier = QualityTier.Impure,
            residueRatio = lastResult.residueRatio
        };

        stackData.SetCustomAspectProfile(customAspectProfile);

        return new ItemSlot(residueItemData, 1, stackData);
    }

    public void OnInteract(GameObject interactor, ulong interacterId)
    {
        ExecuteCircle();
    }

    private void ClearInputPorts()
    {
        foreach (var item in inputPorts)
        {
            item.Clear();
        }
    }
}
