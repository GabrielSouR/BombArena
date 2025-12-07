using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputInitializer : MonoBehaviour
{
    [SerializeField] private string actionMapName = "Player";

    private void Awake()
    {
        var pi = GetComponent<PlayerInput>();
        if (pi != null && !string.IsNullOrEmpty(actionMapName))
        {
            pi.SwitchCurrentActionMap(actionMapName);
            Debug.Log($"{name}: usando action map '{actionMapName}'");
        }
    }
}
