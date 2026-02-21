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
        private bool showPlayerList = true;

        // Player list change tracking
        private bool isSubscribed;
        private string lastChangeType = "None";
        private string lastChangeTime = "--";
        private int lastChangeIndex = -1;
        private int changeCount;

        [MenuItem("Window/Multiplayer Debugger")]
        public static void ShowWindow()
        {
            GetWindow<MultiplayerDebugWindow>("Multiplayer Debug");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            TrySubscribe();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            TryUnsubscribe();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                isSubscribed = false;
                changeCount = 0;
                lastChangeType = "None";
                lastChangeTime = "--";
                lastChangeIndex = -1;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                TryUnsubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (isSubscribed) return;
            if (!Application.isPlaying) return;
            if (GameFlowManager.Instance == null) return;

            GameFlowManager.Instance.OnPlayerListChanged += OnPlayerListChanged;
            isSubscribed = true;
        }

        private void TryUnsubscribe()
        {
            if (!isSubscribed) return;
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnPlayerListChanged -= OnPlayerListChanged;
            }
            isSubscribed = false;
        }

        private void OnPlayerListChanged(NetworkListEvent<PlayerNetworkData> changeEvent)
        {
            lastChangeType = changeEvent.Type.ToString();
            lastChangeIndex = changeEvent.Index;
            lastChangeTime = System.DateTime.Now.ToString("HH:mm:ss.fff");
            changeCount++;
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && !isSubscribed)
            {
                TrySubscribe();
            }
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
            DrawPlayerListStatus();

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

        private void DrawPlayerListStatus()
        {
            showPlayerList = EditorGUILayout.Foldout(showPlayerList, "Player List (NetworkPlayerData)", true, EditorStyles.foldoutHeader);
            if (!showPlayerList) return;

            EditorGUI.indentLevel++;

            if (GameFlowManager.Instance == null)
            {
                EditorGUILayout.HelpBox("GameFlowManager.Instance is NULL", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            var gm = GameFlowManager.Instance;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Change tracking info
            EditorGUILayout.LabelField("Change Tracking", EditorStyles.boldLabel);
            DrawStatusLabel("Subscribed", isSubscribed);
            DrawLabel("Total Changes", changeCount.ToString());
            DrawLabel("Last Change Type", lastChangeType);
            DrawLabel("Last Change Index", lastChangeIndex >= 0 ? lastChangeIndex.ToString() : "--");
            DrawLabel("Last Change Time", lastChangeTime);

            EditorGUILayout.Space();

            // Player list
            if (gm.PlayerList == null)
            {
                EditorGUILayout.HelpBox("PlayerList is NULL (not yet initialized)", MessageType.Info);
            }
            else if (gm.PlayerList.Count == 0)
            {
                EditorGUILayout.HelpBox("PlayerList is empty - No players connected", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Players ({gm.PlayerList.Count})", EditorStyles.boldLabel);

                for (int i = 0; i < gm.PlayerList.Count; i++)
                {
                    var p = gm.PlayerList[i];

                    // Highlight the last changed index
                    var bgColor = GUI.backgroundColor;
                    if (i == lastChangeIndex)
                    {
                        GUI.backgroundColor = new Color(1f, 1f, 0.5f, 1f);
                    }

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUI.backgroundColor = bgColor;

                    // Header with status indicator
                    EditorGUILayout.BeginHorizontal();
                    var statusColor = p.IsAlive ? Color.green : Color.red;
                    var originalColor = GUI.contentColor;
                    GUI.contentColor = statusColor;
                    EditorGUILayout.LabelField(
                        $"[{i}] {p.PlayerName}",
                        EditorStyles.boldLabel
                    );
                    GUI.contentColor = originalColor;
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;
                    DrawLabel("Client ID", p.ClientId.ToString());
                    DrawLabel("Player Name", p.PlayerName.ToString());
                    DrawLabel("Score", p.Score.ToString());
                    DrawStatusLabel("Is Alive", p.IsAlive);
                    EditorGUI.indentLevel--;

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space();

            // Reset button
            if (GUILayout.Button("Reset Change Counter"))
            {
                changeCount = 0;
                lastChangeType = "None";
                lastChangeTime = "--";
                lastChangeIndex = -1;
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
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