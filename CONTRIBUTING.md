# Contributing to NEXUS OS

Thank you for your interest in contributing to NEXUS OS!

## Development Guidelines

1. **Architecture Boundaries**: Keep clean boundaries between Core, Infrastructure, UI, Apps, and Shell.
2. **Honesty & Stability**: Never fake progress or features. Implement real verification and error handling.
3. **Security First**: Do not circumvent Windows security mechanisms, SmartScreen, or UAC.
4. **Testing**: Add unit and integration tests in `NexusOS.Tests` for all new subsystems. Ensure `dotnet test` passes before opening pull requests.
5. **Code Style**: Follow modern C# conventions, nullable reference annotations, and clear XML doc comments on public interfaces.
