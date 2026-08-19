# Contributing to Unity NuGet

Thank you for your interest in contributing to **Unity NuGet**! We welcome bug reports, feature requests, documentation improvements, and pull requests.

## How to Contribute

### Reporting Bugs & Requesting Features
- Please check the [existing issues](https://github.com/ADK-OS/Unity-NuGet/issues) before submitting a new one.
- When filing a bug report, include your Unity version, OS, reproduction steps, and console logs.
- For feature suggestions, please explain the use case and why it benefits Unity NuGet users.

### Submitting Pull Requests
1. Fork the repository on GitHub.
2. Create a feature or fix branch from `main`:
   ```bash
   git checkout -b feature/my-new-feature
   ```
3. Implement your changes adhering to:
   - Clean, readable C# following existing project conventions and namespaces (`ADKUnityNuGet`).
   - Standard [Semantic Versioning 2.0.0](https://semver.org/).
   - Minimal external dependencies (prefer standard .NET and Unity Editor APIs).
4. Commit with clear, descriptive messages:
   ```bash
   git commit -m "feat: add support for custom package sources"
   ```
5. Push to your fork and submit a Pull Request to `main`.

## Code Style & Guidelines
- Follow standard C# naming conventions.
- Keep assemblies scoped to Editor via `ADKUnityNuget.Editor.asmdef`.
- Update `CHANGELOG.md` for any user-facing changes.

## License
By contributing to Unity NuGet, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).
