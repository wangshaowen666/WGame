# WFramework

A Unity framework design featuring common design patterns and utility classes, suitable for building foundational architecture in game development.

## Features

- **Logging System**: Supports colored output and log level control
- **Object Pool**: Provides efficient recycling mechanisms for class objects and Unity objects
- **Finite State Machine**: Supports state transitions and data passing
- **Coroutine Management**: Unified coroutine scheduling interface
- **Resource Loading**: Encapsulates Addressables resource loading logic
- **Hot Update**: Supports hot update workflow for resources
- **Performance Profiling**: Provides time-based performance analysis tools

## Directory Structure

```
├── Base            // Base functionality modules
├── Module          // Core framework modules
├── Profiler        // Performance analysis tools
├── Test            // Functional test cases
└── Util            // Utility class extensions
```

## Quick Start

1. Import this framework into your Unity project's Assets directory
2. Create an empty GameObject in the Hierarchy view and attach the `HotUpdater` component to initiate the hot update process
3. Load resources using `ResMgr.Instance.LoadSync<T>("key")`
4. Start workflow management via `Procedure.Instance.RunProcedure<YourProcedure>()`

## Usage Example

```csharp
// Define a state machine
public class GameStartState : FsmState 
{
    public override void OnEnter()
    {
        Log.Info("Entering game start state");
    }
}

// Create a state machine
var fsm = Fsm.Create(new List<FsmState> { 
    new GameStartState(), 
    new MainMenuState() 
});
fsm.ChangeState<GameStartState>();
```

## Contribution Guidelines

1. Fork the repository
2. Create a feature branch
3. Implement the feature and add unit tests
4. Submit a PR with a complete change description
5. Wait for code review

## License

This project is licensed under the MIT License. See the LICENSE file for full terms.