using System.Collections;
using BlackCommission.WorkOrders.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host-authoritative dot-matrix printer. HQ mode automatically prints the newly accepted task;
/// mission mode sells reprints. A completed sheet remains in the output slot until a player tears
/// it free, at which point the same server transaction files it into that player's hotbar.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class WorkOrderPrinter : NetworkBehaviour, IInteractable
{
    [SerializeField] GameObject paperPreview;

    public readonly NetworkVariable<int> PrintState = new((int)WorkOrderPrintState.Idle,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> IsReprintStation = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<FixedString64Bytes> PrintedTaskId = new(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    WorkOrderConfig config;
    Coroutine printing;
    string localInteractionFeedback;
    float localInteractionFeedbackUntil;

    WorkOrderPrintState State => (WorkOrderPrintState)PrintState.Value;
    float PrintDuration => config != null ? config.printDuration : 3.5f;
    int ReprintCost => config != null ? config.reprintCost : 5;
    float TearRange => config != null ? config.tearInteractRange : 2f;

    public static WorkOrderPrinter HqPrinter
    {
        get
        {
            foreach (WorkOrderPrinter p in Object.FindObjectsByType<WorkOrderPrinter>(FindObjectsSortMode.None))
                if (p != null && !p.IsReprintStation.Value) return p;
            return null;
        }
    }

    public static bool IsOutboundOrderTorn => HqPrinter != null && HqPrinter.State == WorkOrderPrintState.Torn;

    public static string OutboundStatusHint
    {
        get
        {
            WorkOrderPrinter printer = HqPrinter;
            if (printer == null) return "OFFICE PRINTER OFFLINE";
            return printer.State switch
            {
                WorkOrderPrintState.Idle => "OFFICE PRINTER WAITING FOR JOB",
                WorkOrderPrintState.Printing => "WORK ORDER IS PRINTING INSIDE",
                WorkOrderPrintState.ReadyToTear => "WORK ORDER READY — TEAR IT FROM PRINTER",
                _ => "WORK ORDER COLLECTED"
            };
        }
    }

    void Awake()
    {
        config = Resources.Load<WorkOrderConfig>("Config/WorkOrderConfig");
        if (paperPreview == null)
        {
            Transform found = transform.Find("PaperPreview");
            if (found != null) paperPreview = found.gameObject;
        }
    }

    public override void OnNetworkSpawn()
    {
        PrintState.OnValueChanged += HandleStateChanged;
        HandleStateChanged(PrintState.Value, PrintState.Value);
    }

    public override void OnNetworkDespawn()
    {
        PrintState.OnValueChanged -= HandleStateChanged;
    }

    public void ConfigureServer(bool reprintStation)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        IsReprintStation.Value = reprintStation;
    }

    void Update()
    {
        if (!IsServer || IsReprintStation.Value || State != WorkOrderPrintState.Idle) return;
        OfficeTaskDefinition task = MvpMissionRuntime.SelectedTask;
        if (task != null) BeginPrint(task);
    }

    public string InteractHint
    {
        get
        {
            if (!string.IsNullOrEmpty(localInteractionFeedback) && Time.unscaledTime < localInteractionFeedbackUntil)
                return localInteractionFeedback;
            return State switch
            {
                WorkOrderPrintState.Printing => "PRINTING WORK ORDER...",
                WorkOrderPrintState.ReadyToTear => "TEAR WORK ORDER",
                WorkOrderPrintState.Torn when !IsReprintStation.Value => "WORK ORDER TAKEN",
                _ when IsReprintStation.Value => $"REPRINT WORK ORDER  {ReprintCost}G",
                _ => "WAITING FOR ACCEPTED COMMISSION"
            };
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player == null) return;
        InteractServerRpc();
    }

    public void OnInteractEnd(PlayerController player) { }

    [ServerRpc(RequireOwnership = false)]
    void InteractServerRpc(ServerRpcParams rpcParams = default)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var client)) return;
        NetworkObject playerObject = client.PlayerObject;
        if (playerObject == null)
            return;
        if (Vector3.Distance(playerObject.transform.position, transform.position) > TearRange + 0.75f)
        {
            RejectInteractionClientRpc(rpcParams.Receive.SenderClientId, "MOVE CLOSER TO THE PRINTER");
            return;
        }

        if (State == WorkOrderPrintState.ReadyToTear)
        {
            if (!playerObject.TryGetComponent<PlayerHotbar>(out var hotbar))
            {
                RejectInteractionClientRpc(rpcParams.Receive.SenderClientId, "HOTBAR OFFLINE");
                return;
            }
            if (!hotbar.GrantItemServer(MvpHotbarItemId.WorkOrder, 1))
            {
                RejectInteractionClientRpc(rpcParams.Receive.SenderClientId, "HOTBAR FULL — FREE ONE SLOT");
                return;
            }
            if (IsReprintStation.Value)
            {
                PrintState.Value = (int)WorkOrderPrintState.Idle;
                PrintedTaskId.Value = default;
            }
            else
            {
                PrintState.Value = (int)WorkOrderPrintState.Torn;
            }
            return;
        }

        if (!IsReprintStation.Value || State != WorkOrderPrintState.Idle) return;
        OfficeTaskDefinition task = MvpMissionRuntime.ActiveTask;
        if (task == null) return;
        if (CompanyData.Current.Funds < ReprintCost)
        {
            RejectClientRpc("INSUFFICIENT FUNDS — PAPER AND RIBBON COST MONEY");
            return;
        }

        CompanyData.Current.Funds -= ReprintCost;
        CompanyData.Save();
        SyncFundsClientRpc(CompanyData.Current.Funds);
        BeginPrint(task);
    }

    void BeginPrint(OfficeTaskDefinition task)
    {
        if (!IsServer || task == null || State != WorkOrderPrintState.Idle) return;
        PrintedTaskId.Value = new FixedString64Bytes(task.taskId ?? string.Empty);
        PrintState.Value = (int)WorkOrderPrintState.Printing;
        if (printing != null) StopCoroutine(printing);
        printing = StartCoroutine(PrintRoutine());
    }

    IEnumerator PrintRoutine()
    {
        // The fixed mid-print pause is the approved "bad printer" beat, not random failure.
        float firstLeg = PrintDuration * 0.56f;
        yield return new WaitForSeconds(firstLeg);
        yield return new WaitForSeconds(0.35f);
        yield return new WaitForSeconds(Mathf.Max(0f, PrintDuration - firstLeg - 0.35f));
        PrintState.Value = (int)WorkOrderPrintState.ReadyToTear;
        printing = null;
    }

    void HandleStateChanged(int previous, int current)
    {
        if (paperPreview != null)
            paperPreview.SetActive((WorkOrderPrintState)current == WorkOrderPrintState.ReadyToTear);
    }

    [ClientRpc]
    void SyncFundsClientRpc(int funds) => CompanyData.Current.Funds = funds;

    [ClientRpc]
    void RejectClientRpc(string reason) => Debug.LogWarning($"[WorkOrderPrinter] {reason}");

    [ClientRpc]
    void RejectInteractionClientRpc(ulong recipientClientId, string reason)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != recipientClientId) return;
        localInteractionFeedback = reason;
        localInteractionFeedbackUntil = Time.unscaledTime + 2.5f;
        Debug.LogWarning($"[WorkOrderPrinter] {reason}");
    }
}
