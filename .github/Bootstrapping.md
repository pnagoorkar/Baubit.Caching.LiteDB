# Baubit Project Bootstrap Instructions

This document provides comprehensive instructions for GitHub Copilot to bootstrap new Baubit.X or Baubit.X.DI projects from the template repository.

---

## Project Types

### Type 1: Core Library (Baubit.X)
A standalone library that provides specific functionality (e.g., Baubit.Caching, Baubit.Mediation).

### Type 2: DI Extension (Baubit.X.DI)
A dependency injection module that integrates a Baubit library with Microsoft.Extensions.DependencyInjection (e.g., Baubit.Caching.DI, Baubit.Logging.DI).

---

## Step-by-Step Bootstrap Process

### Step 1: Update CircleCI Configuration

**File:** `.circleci/config.yml`

Replace all instances of `<YOUR_SOLUTION_NAME>` and `<YOUR_PROJECT_NAME>` with the actual project name.

**Example for Baubit.MyComponent.DI:**
```yaml
workflows:
  version: 2
  build:
    jobs:
      - build:
          dotnet_solution_folder: "Baubit.MyComponent.DI"
          dotnet_project_name: "Baubit.MyComponent.DI"
```

### Step 2: Update Copilot Instructions

**File:** `.github/copilot-instructions.md`

Copy the standard Baubit Copilot instructions (see template below). No project-specific changes needed.

### Step 3: Create Solution File

**File:** `Baubit.{ProjectName}.slnx`

```xml
<Solution>
  <Folder Name="/SolutionItems/">
    <File Path=".circleci/config.yml" />
    <File Path=".github/copilot-instructions.md" />
  </Folder>
  <Project Path="Baubit.{ProjectName}.Test/Baubit.{ProjectName}.Test.csproj" />
  <Project Path="Baubit.{ProjectName}/Baubit.{ProjectName}.csproj" />
</Solution>
```

### Step 4: Create Main Project

**Directory:** `Baubit.{ProjectName}/`

#### 4a. Create .csproj File

**File:** `Baubit.{ProjectName}/Baubit.{ProjectName}.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <AssemblyVersion>$(AssemblyVersion)</AssemblyVersion>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageLicenseFile>LICENSE</PackageLicenseFile>
    <PreserveCompilationContext>true</PreserveCompilationContext>
    <Deterministic>true</Deterministic>
    <RepositoryUrl>https://github.com/pnagoorkar/Baubit.{ProjectName}</RepositoryUrl>
    <Authors>Prashant Nagoorkar</Authors>
    <SignAssembly>$(SignAssembly)</SignAssembly>
    <AssemblyOriginatorKeyFile>$(KeyFilePath)</AssemblyOriginatorKeyFile>
    <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>
    <Description>{Project description}</Description>
    <PackageTags>{semicolon-separated-tags};netstandard;baubit</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="../README.md" Pack="true" PackagePath="." />
    <None Include="../LICENSE" Pack="true" PackagePath="." />
  </ItemGroup>

  <ItemGroup>
    <!-- Add package references here -->
  </ItemGroup>

</Project>
```

#### 4b. For DI Projects - Create Configuration Class

**File:** `Baubit.{ProjectName}/Configuration.cs`

```csharp
using Baubit.DI;

namespace Baubit.{ProjectName}
{
    /// <summary>
    /// Configuration class for the {ProjectName} module.
    /// {Describe what the configuration controls}
    /// </summary>
    public class Configuration : AConfiguration
    {
        // Add configuration properties with XML documentation
        // Example:
        // /// <summary>
        // /// Gets or sets {property description}.
        // /// Defaults to {default value}.
        // /// </summary>
        // public bool SomeOption { get; set; } = false;
    }
}
```

#### 4c. For DI Projects - Create Module Class

**File:** `Baubit.{ProjectName}/Module.cs`

```csharp
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Baubit.{ProjectName}
{
    /// <summary>
    /// DI module for registering {service description} with Microsoft.Extensions.DependencyInjection.
    /// {Describe what the module does}
    /// </summary>
    public class Module : AModule<Configuration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Module"/> class from configuration.
        /// </summary>
        /// <param name="configuration">The configuration section to bind settings from.</param>
        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Module"/> class with strongly-typed configuration.
        /// </summary>
        /// <param name="configuration">The strongly-typed configuration for this module.</param>
        /// <param name="nestedModules">Optional list of nested modules this module depends on.</param>
        public Module(Configuration configuration, List<IModule> nestedModules = null) : base(configuration, nestedModules)
        {
        }

        /// <summary>
        /// Registers services with the specified service collection.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        public override void Load(IServiceCollection services)
        {
            // Register services based on Configuration
            // Example:
            // services.AddSingleton<IMyService, MyService>();

            base.Load(services);
        }
    }
}
```

### Step 5: Create Test Project

**Directory:** `Baubit.{ProjectName}.Test/`

#### 5a. Create Test .csproj File

**File:** `Baubit.{ProjectName}.Test/Baubit.{ProjectName}.Test.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Baubit.{ProjectName}\Baubit.{ProjectName}.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

#### 5b. For DI Projects - Create Module Tests

**File:** `Baubit.{ProjectName}.Test/Module/Test.cs`

```csharp
using Baubit.DI;
using Baubit.DI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.{ProjectName}.Test.Module
{
    /// <summary>
    /// Unit tests for <see cref="{ProjectName}.Module"/>
    /// </summary>
    public class Test
    {
        [Fact]
        public void Constructor_WithIConfiguration_CreatesModule()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Add configuration key-value pairs
            });
            var config = configBuilder.Build();

            // Act
            var module = new {ProjectName}.Module(config);

            // Assert
            Assert.NotNull(module);
            Assert.NotNull(module.Configuration);
        }

        [Fact]
        public void Constructor_WithConfiguration_CreatesModule()
        {
            // Arrange
            var configuration = new {ProjectName}.Configuration
            {
                // Set configuration properties
            };

            // Act
            var module = new {ProjectName}.Module(configuration);

            // Assert
            Assert.NotNull(module);
        }

        [Fact]
        public void Constructor_WithNestedModules_CreatesModule()
        {
            // Arrange
            var configuration = new {ProjectName}.Configuration();
            var nestedModules = new List<Baubit.DI.IModule>();

            // Act
            var module = new {ProjectName}.Module(configuration, nestedModules);

            // Assert
            Assert.NotNull(module);
            Assert.NotNull(module.NestedModules);
        }

        [Fact]
        public void Load_RegistersServices_Successfully()
        {
            // Arrange
            var configuration = new {ProjectName}.Configuration
            {
                // Set configuration properties
            };
            var module = new {ProjectName}.Module(configuration);
            var services = new ServiceCollection();

            // Act
            module.Load(services);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            // Verify services are registered correctly
        }

        [Fact]
        public void Load_WithComponentBuilder_RegistersServices()
        {
            // Arrange & Act
            var result = ComponentBuilder.CreateNew()
                                         .WithModule<{ProjectName}.Module, {ProjectName}.Configuration>(config =>
                                         {
                                             // Configure module
                                         })
                                         .BuildServiceProvider();

            // Assert
            Assert.True(result.IsSuccess);
            // Verify services
        }

        [Fact]
        public void Configuration_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var configuration = new {ProjectName}.Configuration();

            // Assert
            // Verify default values
        }

        [Fact]
        public void Module_IsSubclassOfAModule()
        {
            // Arrange
            var configuration = new {ProjectName}.Configuration();

            // Act
            var module = new {ProjectName}.Module(configuration);

            // Assert
            Assert.IsAssignableFrom<Baubit.DI.AModule<{ProjectName}.Configuration>>(module);
        }
    }
}
```

### Step 6: Update README.md

**File:** `README.md`

Follow this structure:

```markdown
# Baubit.{ProjectName}

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.{ProjectName}/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.{ProjectName})<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.{ProjectName}.svg)](https://www.nuget.org/packages/Baubit.{ProjectName}/)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)<br/>
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.{ProjectName}/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.{ProjectName})

## Overview

{Describe what the project does and what problem it solves}

## Installation

\`\`\`bash
dotnet add package Baubit.{ProjectName}
\`\`\`

Or via NuGet Package Manager:

\`\`\`
Install-Package Baubit.{ProjectName}
\`\`\`

## Quick Start

{Include three patterns for DI projects:}

### Pattern 1: Modules from appsettings.json

{JSON configuration example}

### Pattern 2: Modules from Code (IComponent)

{Code example using IComponent}

### Pattern 3: Hybrid Loading (appsettings.json + IComponent)

{Hybrid example}

### Using AddModule Directly

{Direct ServiceCollection example}

## Features

{List key features}

## API Reference

### Configuration

{Document Configuration class properties}

### Module (for DI projects)

{Document Module class}

### Usage Examples

{Provide detailed examples}

## Contributing

Contributions are welcome. Open an issue or submit a pull request.

## License

MIT License - see [LICENSE](LICENSE) file for details.
```

---

## Required Package References

### For DI Extension Projects (Baubit.X.DI)

Always include:
```xml
<PackageReference Include="Baubit.DI.Extensions" Version="2025.49.3" />
```

Add domain-specific packages as needed.

### For Core Library Projects (Baubit.X)

Minimize dependencies. Only add what's essential for the functionality.

---

## Post-Bootstrap Checklist

After bootstrapping, verify:

- [ ] Solution file created: `Baubit.{ProjectName}.slnx`
- [ ] Main project directory created: `Baubit.{ProjectName}/`
- [ ] Main .csproj file created with correct settings
- [ ] Configuration class created (for DI projects)
- [ ] Module class created (for DI projects)
- [ ] Test project directory created: `Baubit.{ProjectName}.Test/`
- [ ] Test .csproj file created
- [ ] Test files created in `{ClassName}/Test.cs` format
- [ ] README.md updated with project-specific content
- [ ] CircleCI config.yml updated with project names
- [ ] Solution builds successfully: `dotnet build {ProjectName}.slnx`
- [ ] All tests pass: `dotnet test {ProjectName}.slnx`

---

## External Setup Required (Manual)

After repository setup, the user must:

1. **Add Codecov Token in CircleCI Context**
   - Variable: `CODECOV_TOKEN_{PROJECT_NAME}` (dots replaced with underscores)
   - Example: `CODECOV_TOKEN_Baubit_Logging_DI`

2. **Configure GitHub Branch Protection**
   - Protect `master` and `release` branches
   - Require status checks

3. **Import in Codecov.io**
   - Add repository for code coverage tracking

4. **Import in Snyk.io**
   - Add repository for vulnerability scanning

---

## Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Namespace | `Baubit.{ComponentName}` | `Baubit.Logging.DI` |
| Solution file | `Baubit.{ComponentName}.slnx` | `Baubit.Logging.DI.slnx` |
| Main project | `Baubit.{ComponentName}/` | `Baubit.Logging.DI/` |
| Test project | `Baubit.{ComponentName}.Test/` | `Baubit.Logging.DI.Test/` |
| Configuration class | `Configuration` | `Configuration.cs` |
| Module class | `Module` | `Module.cs` |
| Test class | `Test` in `{ClassName}/Test.cs` | `Module/Test.cs` |

---

## Key Patterns to Follow

1. **Configuration inherits from `AConfiguration`**
2. **Module inherits from `AModule<Configuration>`**
3. **Module has two constructors**: `IConfiguration` and `Configuration`
4. **Module overrides `Load(IServiceCollection services)`**
5. **Always call `base.Load(services)` at end of Load method**
6. **All public APIs have XML documentation**
7. **Tests use xUnit with AAA pattern**
8. **Test names follow `MethodName_Scenario_ExpectedBehavior`**

Look at Baubit.* repositories for existing patterns in case of ambiguity/confusion.

Delete this file from the repository once bootstrapping is complete.
