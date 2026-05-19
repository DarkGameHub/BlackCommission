public interface IInteractable
{
    string InteractHint { get; }
    void OnInteractStart(PlayerController player);
    void OnInteractEnd(PlayerController player);
}
