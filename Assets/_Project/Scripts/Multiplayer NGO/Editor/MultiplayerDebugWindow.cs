using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Multiplayer.Editor
{
    public class MultiplayerDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showNetwork = true;
        private bool showMatchmaking = true;
        private bool showGameFlow = true;

        [MenuItem("Window/Multiplayer Debugger")]
        public static void ShowWindow()
        {
            GetWindow<MultiplayerDebugWindow>("Multiplayer Debug");
        }

        private void OnInspectorUpdate()
        {
            // Force UI to repaint every frame for real-time updates
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("Multiplayer Debug Console", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see debug info.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawNetworkStatus();
            DrawMatchmakingStatus();
            DrawGameFlowStatus();

            EditorGUILayout.EndScrollView();
        }

        private void DrawNetworkStatus()
        {
            showNetwork = EditorGUILayout.Foldout(showNetwork, "Network Manager", true, EditorStyles.foldoutHeader);
            if (showNetwork)
            {
                EditorGUI.indentLevel++;
                if (NetworkManager.Singleton == null)
                {
                    EditorGUILayout.HelpBox("NetworkManager.Singleton is NULL", MessageType.Error);
                }
                else
                {
                    var nm = NetworkManager.Singleton;
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    DrawStatusLabel("Is Listening", nm.IsListening);
                    DrawStatusLabel("Is Host", nm.IsHost);
                    DrawStatusLabel("Is Server", nm.IsServer);
                    DrawStatusLabel("Is Client", nm.IsClient);
                    DrawStatusLabel("Is Connected Client", nm.IsConnectedClient);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Local Client ID", nm.LocalClientId.ToString());
                    
                    if (nm.IsServer)
                    {
                        EditorGUILayout.LabelField($"Connected Clients: {nm.ConnectedClientsIds.Count}");
                        foreach (var id in nm.ConnectedClientsIds)
                        {
                            EditorGUILayout.LabelField($"- Client {id}");
                        }
                    }

                    if (GUILayout.Button("Shutdown Network"))
                    {
                        nm.Shutdown();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        private void DrawMatchmakingStatus()
        {
            showMatchmaking = EditorGUILayout.Foldout(showMatchmaking, "Matchmaking Manager", true, EditorStyles.foldoutHeader);
            if (showMatchmaking)
            {
                EditorGUI.indentLevel++;
                if (MatchmakingManager.Instance == null)
                {
                    EditorGUILayout.HelpBox("MatchmakingManager.Instance is NULL", MessageType.Error);
                }
                else
                {
                    var mm = MatchmakingManager.Instance;
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    DrawLabel("State", mm.IsMatchingInProgress ? "Matching In Progress..." : "Idle/Ready");
                    DrawStatusLabel("In Lobby", mm.IsInLobby);
                    DrawStatusLabel("Is Lobby Host", mm.IsLobbyHost);

                    if (mm.CurrentLobby != null)
                    {
                        EditorGUILayout.LabelField("Lobby ID", mm.CurrentLobby.Id);
                        EditorGUILayout.LabelField("Lobby Code", mm.CurrentLobby.LobbyCode);
                        EditorGUILayout.LabelField("Max Players", mm.CurrentLobby.MaxPlayers.ToString());
                        
                        if (mm.CurrentLobby.Data != null)
                        {
                            if (mm.CurrentLobby.Data.TryGetValue("joinCode", out var joinCode))
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Relay Join Code", joinCode.Value);
                                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                                {
                                    GUIUtility.systemCopyBuffer = joinCode.Value;
                                    Debug.Log("Join code copied to clipboard!");
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }

                    // Reflection/Private fields access for debugging if needed (Optional)
                    // For now, public properties are enough.
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Force Leave Lobby"))
                    {
                        mm.LeaveLobby().Forget();
                    }
                    if (GUILayout.Button("Force Quick Match"))
                    {
                        mm.StartQuickMatchmaking().Forget();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Internal State (Reflection)", EditorStyles.boldLabel);
                    DrawPrivateField(mm, "hostWaitTimer");
                    DrawPrivateField(mm, "gracePeriodTimer");
                    DrawPrivateField(mm, "isWaitingForPlayers");
                    DrawPrivateField(mm, "isInGracePeriod");
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        private void DrawPrivateField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(target);
                EditorGUILayout.LabelField(fieldName, value != null ? value.ToString() : "null");
            }
            else
            {
                EditorGUILayout.LabelField(fieldName, "Not Found");
            }
        }

        private void DrawGameFlowStatus()
        {
            showGameFlow = EditorGUILayout.Foldout(showGameFlow, "Game Flow Manager", true, EditorStyles.foldoutHeader);
            if (showGameFlow)
            {
                EditorGUI.indentLevel++;
                if (GameFlowManager.Instance == null)
                {
                    EditorGUILayout.HelpBox("GameFlowManager.Instance is NULL", MessageType.Warning);
                }
                else
                {
                    var gm = GameFlowManager.Instance;
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // Check if NetworkVariable is initialized
                    try
                    {
                        EditorGUILayout.LabelField("Current Game State", gm.CurrentGameState.Value.ToString(), EditorStyles.boldLabel);
                    }
                    catch
                    {
                        EditorGUILayout.LabelField("Current Game State", "Not Initialized (NetworkVar)");
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Player Count: {gm.PlayerCount}");

                    if (gm.PlayerList != null)
                    {
                        EditorGUILayout.LabelField("Player List:", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < gm.PlayerList.Count; i++)
                        {
                            var p = gm.PlayerList[i];
                            var status = p.IsAlive ? "Alive" : "Dead";
                            var color = p.IsAlive ? Color.green : Color.red;
                            
                            var originalColor = GUI.contentColor;
                            GUI.contentColor = color;
                            EditorGUILayout.LabelField($"[{p.ClientId}] {p.PlayerName} - Score: {p.Score} - {status}");
                            GUI.contentColor = originalColor;
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space();
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        EditorGUILayout.LabelField("Server Actions", EditorStyles.boldLabel);
                        if (GUILayout.Button("Start Game (RPC)"))
                        {
                            gm.StartGameServerRpc();
                        }
                        if (GUILayout.Button("Return to Menu (RPC)"))
                        {
                            gm.ReturnToMenuServerRpc();
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawStatusLabel(string label, bool status)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            var originalColor = GUI.color;
            GUI.color = status ? Color.green : Color.red;
            GUILayout.Box(status ? "TRUE" : "FALSE", GUILayout.Width(60));
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLabel(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }
    }
}