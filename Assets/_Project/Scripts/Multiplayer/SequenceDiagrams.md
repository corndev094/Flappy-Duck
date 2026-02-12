## Multiplayer System - Sequence Diagrams

### 1. Quick Match Flow
```mermaid
sequenceDiagram
    participant Player
    participant MatchmakingManager
    participant LobbyService
    participant RelayService
    participant GameFlowManager

    Player->>MatchmakingManager: StartQuickMatchmaking()
    
    alt Try Join Lobby
        MatchmakingManager->>LobbyService: QuickJoinLobby()
        MatchmakingManager->>RelayService: JoinRelay()
        MatchmakingManager->>MatchmakingManager: Join as Client
    else Create New Lobby
        MatchmakingManager->>RelayService: CreateRelayAllocation()
        MatchmakingManager->>LobbyService: CreateLobby()
        MatchmakingManager->>MatchmakingManager: Start as Host
        MatchmakingManager->>MatchmakingManager: Wait for Players
    end
    
    MatchmakingManager-->>GameFlowManager: Players Connected
    GameFlowManager->>GameFlowManager: StartGame()
```

### 2. Custom Match with Filters
```mermaid
sequenceDiagram
    participant Player
    participant MatchmakingFilterUI
    participant MatchmakingManager
    participant LobbyService

    Player->>MatchmakingFilterUI: Set Filters (Mode, Level, Region)
    MatchmakingFilterUI->>MatchmakingManager: StartMatchmaking(filter)
    
    MatchmakingManager->>LobbyService: QueryLobbies(filter)
    LobbyService-->>MatchmakingManager: Matching Lobbies
    
    alt Found Lobby
        MatchmakingManager->>MatchmakingManager: Join as Client
    else No Lobby Found
        MatchmakingManager->>MatchmakingManager: Create Lobby as Host
        MatchmakingManager->>MatchmakingManager: Wait for Players
    end
```

### 3. Game Start Flow
```mermaid
sequenceDiagram
    participant MatchmakingManager
    participant GameFlowManager
    participant Players

    MatchmakingManager->>MatchmakingManager: Min Players Reached
    MatchmakingManager->>MatchmakingManager: Grace Period Wait
    MatchmakingManager->>GameFlowManager: StartGameServerRpc()
    
    GameFlowManager->>GameFlowManager: Change State: Loading
    GameFlowManager->>GameFlowManager: Reset Player Data
    GameFlowManager->>Players: NotifyGameStartedClientRpc()
    GameFlowManager->>GameFlowManager: Change State: Playing
```

### 4. Player Death & Game Over
```mermaid
sequenceDiagram
    participant Player
    participant GameFlowManager
    participant UIResultPanel

    Player->>GameFlowManager: NotifyPlayerDied()
    GameFlowManager->>GameFlowManager: Mark Player Dead
    GameFlowManager->>GameFlowManager: CheckGameOver()
    
    alt Game Over (<=1 alive)
        GameFlowManager->>GameFlowManager: Change State: GameOver
        GameFlowManager->>UIResultPanel: ShowResult(ranking)
        GameFlowManager->>Player: DeclareWinnerClientRpc()
    end
```

### 5. Player Connection Management
```mermaid
sequenceDiagram
    participant NetworkManager
    participant GameFlowManager
    participant MatchmakingManager

    alt Player Connects
        NetworkManager->>GameFlowManager: OnClientConnected
        GameFlowManager->>GameFlowManager: AddPlayer()
    else Player Disconnects
        NetworkManager->>GameFlowManager: OnClientDisconnected
        GameFlowManager->>GameFlowManager: RemovePlayer()
        GameFlowManager->>GameFlowManager: CheckGameOver()
    else Host Leaves
        MatchmakingManager->>MatchmakingManager: DeleteLobby
        MatchmakingManager->>MatchmakingManager: Disconnect All
    end
```

### 6. Score Update
```mermaid
sequenceDiagram
    participant Player
    participant GameFlowManager

    Player->>GameFlowManager: AddScore(points)
    GameFlowManager->>GameFlowManager: Update Player Score
    Note over GameFlowManager: Score synced via NetworkList
```

### 7. Leave Game
```mermaid
sequenceDiagram
    participant Player
    participant UIResultPanel
    participant GameFlowManager
    participant MatchmakingManager

    Player->>UIResultPanel: Click Close
    UIResultPanel->>GameFlowManager: Disconnect()
    GameFlowManager->>MatchmakingManager: LeaveLobby()
    MatchmakingManager->>NetworkManager: Shutdown()
    GameFlowManager->>GameFlowManager: Return to Menu
```
