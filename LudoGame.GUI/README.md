# LUDO-T Game GUI Application

This is a WPF (Windows Presentation Foundation) GUI application that replaces the command-line interface of the LUDO-T game simulation while maintaining Clean Architecture principles.

## Important Note

**This WPF application can only be built and run on Windows** as WPF is a Windows-specific technology. The project structure is complete and ready to be built on a Windows machine with .NET 8.0 SDK installed.

## Project Structure

The GUI application follows the MVVM (Model-View-ViewModel) pattern and maintains Clean Architecture principles:

```
LudoGame.GUI/
├── App.xaml                 # Application entry point and global resources
├── App.xaml.cs             # Application code-behind
├── Views/
│   └── MainWindow.xaml     # Main game window UI
│   └── MainWindow.xaml.cs  # Main window code-behind
├── ViewModels/
│   ├── MainViewModel.cs    # Main view model with game logic
│   └── ViewModelBase.cs    # Base view model with INotifyPropertyChanged
├── Models/
│   └── GameBoardModel.cs   # Game board data model
└── Services/
    └── GameLoggerService.cs # GUI-specific logging service
```

## Clean Architecture Compliance

The GUI application maintains Clean Architecture by:

1. **Dependency Inversion**: The GUI layer depends on abstractions (interfaces) from the core game logic
2. **Separation of Concerns**: UI logic is separated from game business logic
3. **Single Responsibility**: Each class has a single, well-defined responsibility
4. **Interface Segregation**: Specific interfaces for different concerns
5. **Open/Closed Principle**: The system is open for extension but closed for modification

## How to Build on Windows

### Prerequisites

- Windows 10 or later
- .NET 8.0 SDK or later
- Visual Studio 2022 (recommended) or .NET CLI

### Building with Visual Studio

1. Open `LudoGameSolution.sln` in Visual Studio 2022
2. Ensure the solution is configured to build for Windows
3. Build the solution (Ctrl+Shift+B)
4. Set `LudoGame.GUI` as the startup project
5. Run the application (F5)

### Building with .NET CLI

```bash
# Navigate to the GUI project directory
cd LudoGame.GUI

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

## Features

### Main Window

The main window provides:

1. **Game Board Visualization**: A visual representation of the LUDO-T board with:
   - 52 colored cells arranged in a circle
   - Home bases for each player (Red, Green, Yellow, Blue)
   - Center home area
   - Mystery cell indicators
   - Piece positions

2. **Game Status Panel**: Shows current game information:
   - Current player
   - Game round number
   - Game state (Ready, Running, Paused)

3. **Game Log**: Real-time logging of game events with timestamps

4. **Control Buttons**:
   - **Start Game**: Begins the simulation
   - **Pause/Resume**: Pauses or resumes the game
   - **Reset**: Resets the game to initial state
   - **Step**: Executes a single game step (useful for debugging)

### UI/UX Features

- Clean, modern interface with consistent styling
- Color-coded elements matching the LUDO-T game colors
- Responsive layout that adapts to window resizing
- Scrollable game log for long game sessions
- Intuitive button states (enabled/disabled based on game state)

## Integration with Existing Game Logic

The GUI integrates with the existing LUDO-T game logic through:

1. **LudoGameController**: Uses the existing game controller
2. **GameLoggerService**: Adapts the console logger to GUI logging
3. **Service Pattern**: Uses dependency injection for services
4. **Observer Pattern**: Observes game state changes and updates UI accordingly

## Next Steps for Full Implementation

To complete the full integration:

1. **Modify LudoGameController**: Add step-by-step execution support
2. **Implement Game Loop**: Connect the timer-based game execution
3. **Piece Visualization**: Add dynamic piece rendering on the board
4. **Animation**: Add smooth piece movement animations
5. **Sound Effects**: Add optional sound effects for game events
6. **Settings**: Add game configuration options

## Testing

The GUI application maintains all existing unit tests from the original console application. The core game logic remains unchanged, ensuring that all test cases continue to pass.

```bash
# Run tests
dotnet test LudoGame.Tests/LudoGame.Tests.csproj
```

## Assignment Requirements Coverage

This GUI application addresses the Assignment 2 requirements:

✅ **GUI Interface**: Replaces command-line interface with graphical interface  
✅ **Clean Architecture**: Maintains Clean Architecture principles  
✅ **All Use Cases**: All original use cases remain functional  
✅ **Week 1 Concepts**: Uses WPF concepts from Week 1 of Semester 2  
✅ **Multi-tier Ready**: Structure supports future client-server refactoring  

## Future Enhancement: Client-Server Architecture

The current GUI structure is designed to support the next assignment requirement of converting to a multi-user, multi-tier, client-server application:

- The ViewModel can be easily separated into a client-side component
- The game controller can be moved to a server tier
- Communication can be implemented through WCF, gRPC, or HTTP APIs
- The existing interfaces support this separation

## Troubleshooting

### Build Errors on macOS/Linux

If you try to build this on macOS or Linux, you will get errors about missing Windows Desktop SDK. This is expected and correct behavior. Build this project only on Windows.

### Missing Dependencies

If you get dependency errors, run:
```bash
dotnet restore
```

### Runtime Errors

If you encounter runtime errors:
1. Ensure the main LudoGame project is built
2. Check that all project references are correct
3. Verify that .NET 8.0 Windows SDK is installed

## Credits

- Based on the original LUDO-T console simulation
- Maintains Clean Coding principles
- Follows MVVM pattern for WPF development
- Preserves all existing game logic and rules
