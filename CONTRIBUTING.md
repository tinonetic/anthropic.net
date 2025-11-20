# Contributing to Anthropic.Net

Thank you for your interest in contributing to Anthropic.Net! We welcome contributions from the community to help improve this SDK.

## Development Environment

To get started, ensure you have the following installed:

- **.NET SDK**: Version 8.0 or later (see `global.json`).
- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider.
- **PowerShell**: Required for running build scripts.

## Getting Started

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally:

    ```bash
    git clone https://github.com/YOUR-USERNAME/anthropic.net.git
    cd anthropic.net
    ```

3. **Restore** dependencies and tools:

    ```bash
    dotnet tool restore
    dotnet restore
    ```

## Building and Testing

The project uses [Cake](https://cakebuild.net/) for build automation. You can run the build script using the dotnet tool:

```bash
dotnet cake --target=Build
```

To run tests:

```bash
dotnet cake --target=Test
```

To run the full CI pipeline (Build, Test, Pack):

```bash
dotnet cake --target=Default
```

## Code Style

We enforce code style using `.editorconfig` and StyleCop analyzers.

- **Naming**: PascalCase for public members, camelCase for private fields/parameters.
- **Formatting**: Standard C# conventions (K&R braces, 4-space indentation).
- **Headers**: File headers are not required.
- **Warnings**: Treat warnings as errors is enabled in CI. Ensure your code builds without warnings.

Run `dotnet format` to automatically fix style issues.

## Pull Request Process

1. Create a new branch for your feature or fix:

    ```bash
    git checkout -b feature/my-awesome-feature
    ```

2. Commit your changes with clear, descriptive messages.
3. Push your branch to your fork.
4. Open a Pull Request (PR) against the `main` branch of the `tinonetic/anthropic.net` repository.
5. Ensure all CI checks pass.

## Release Process (Maintainers Only)

Releases are automated via GitHub Actions and MinVer.

1. **Versioning**: Determine the next version number (SemVer).
2. **Tagging**: Create and push a git tag for the new version (e.g., `v2.0.0`).

    ```bash
    git tag v2.0.0
    git push origin v2.0.0
    ```

3. **Publishing**: Go to GitHub Releases, draft a new release for the tag, and click "Publish". This triggers the NuGet publication workflow.

## License

By contributing, you agree that your contributions will be licensed under the project's [MIT License](LICENSE.md).
