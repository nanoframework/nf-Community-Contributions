# System.IO.Streams[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=nanoframework_System.IO.Streams&metric=alert_status)](https://sonarcloud.io/dashboard?id=nanoframework_System.IO.Streams) [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=nanoframework_System.IO.Streams&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=nanoframework_System.IO.Streams) [![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![NuGet](https://img.shields.io/nuget/dt/nanoFramework.System.IO.Streams.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.System.IO.Streams/) [![#yourfirstpr](https://img.shields.io/badge/first--timers--only-friendly-blue.svg)](https://github.com/nanoframework/Home/blob/main/CONTRIBUTING.md) [![Discord](https://img.shields.io/discord/478725473862549535.svg?logo=discord&logoColor=white&label=Discord&color=7289DA)](https://discord.gg/gCyBu8T)

![nanoFramework logo](https://raw.githubusercontent.com/nanoframework/Home/main/resources/logo/nanoFramework-repo-logo.png)

-----

# Welcome to the .NET **nanoFramework** System.IO.Streams Library repository

## Build status

| Component | Build Status | NuGet Package |
|:-|---|---|
| System.IO.Streams | [![Build Status](https://dev.azure.com/nanoframework/System.IO.Streams/_apis/build/status/nanoframework.System.IO.Streams?repoName=nanoframework%2FSystem.IO.Streams&branchName=main)](https://dev.azure.com/nanoframework/System.IO.Streams/_build/latest?definitionId=74&repoName=nanoframework%2FSystem.IO.Streams&branchName=main) | [![NuGet](https://img.shields.io/nuget/v/nanoFramework.System.IO.Streams.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.System.IO.Streams/) |
| System.IO.Streams (preview) | [![Build Status](https://dev.azure.com/nanoframework/System.IO.Streams/_apis/build/status/nanoframework.System.IO.Streams?repoName=nanoframework%2FSystem.IO.Streams&branchName=develop)](https://dev.azure.com/nanoframework/System.IO.Streams/_build/latest?definitionId=74&repoName=nanoframework%2FSystem.IO.Streams&branchName=develop) | [![NuGet](https://img.shields.io/nuget/vpre/nanoFramework.System.IO.Streams.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.System.IO.Streams/) |

## Usage examples

### Using a MemoryStream

Write data using memory as a backing store.

```csharp
using MemoryStream memStream = new MemoryStream(100);

string test = "nanoFramework Test";
memStream.Write(Encoding.UTF8.GetBytes(test), 0, test.Length);
memStream.Flush();
```

Reset stream position.

```csharp
memStream.Seek(0, SeekOrigin.Begin);
```

Read from from stream.

```csharp
byte[] readbuff = new byte[20];
memStream.Read(readbuff, 0, readbuff.Length);

string testResult = new string(Encoding.UTF8.GetChars(readbuff));
```

## Feedback and documentation

For documentation, providing feedback, issues and finding out how to contribute please refer to the [Home repo](https://github.com/nanoframework/Home).

Join our Discord community [here](https://discord.gg/gCyBu8T).

## Credits

The list of contributors to this project can be found at [CONTRIBUTORS](https://github.com/nanoframework/Home/blob/main/CONTRIBUTORS.md).

## License

The **nanoFramework** Class Libraries are licensed under the [MIT license](LICENSE.md).

## Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behaviour in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

### .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
