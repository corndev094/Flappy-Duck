using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class NetworkDebugGUI : MonoBehaviour
{
    private float startTime;
    private bool showDebug;

    void Start()
    {
        startTime = Time.time;
    }

    private void Update() {
        if (Keyboard.current.f1Key.IsPressed())
        {
            showDebug = !showDebug;
        }
    }

    void OnGUI()
    {
        if (!showDebug) return;
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 450, 300), GUI.skin.box);

        GUILayout.Label("=== NGO NETWORK DEBUG ===");
        GUILayout.Label($"Time: {(Time.time - startTime):F1}s");

        GUILayout.Space(5);
        GUILayout.Label($"IsServer: {nm.IsServer}");
        GUILayout.Label($"IsHost: {nm.IsHost}");
        GUILayout.Label($"IsClient: {nm.IsClient}");
        GUILayout.Label($"IsListening: {nm.IsListening}");
        GUILayout.Label($"IsConnectedClient: {nm.IsConnectedClient}");

        GUILayout.Space(5);
        GUILayout.Label($"LocalClientId: {nm.LocalClientId}");
        GUILayout.Label($"ConnectedClients: {nm.ConnectedClients.Count}");

        if (!nm.IsServer && nm.IsClient)
        {
            GUILayout.Space(5);
            GUILayout.Label("Client State:");
            GUILayout.Label(nm.IsConnectedClient ? "CONNECTED" : "CONNECTING...");
        }

        if (nm.IsServer)
        {
            GUILayout.Space(5);
            GUILayout.Label("Server State:");
            GUILayout.Label("RUNNING");
        }

        GUILayout.EndArea();
    }
}
