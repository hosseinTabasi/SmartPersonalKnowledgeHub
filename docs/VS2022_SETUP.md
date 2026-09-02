# Visual Studio 2022 setup

This guide is for a Windows 10 or Windows 11 laboratory PC. The WPF client targets `net8.0-windows` and will not run on Linux.

## 1. Install Visual Studio 2022

1. Download Visual Studio 2022 Community, Professional or Enterprise from Microsoft.
2. In the installer, select the workload **.NET desktop development**.
3. Confirm that **.NET 8.0** (or the .NET desktop SDK that includes .NET 8) is ticked.
4. Finish the install and reboot if the installer requests it.

Optional but useful individual components:

- Git for Windows
- NuGet package manager (included by default)

## 2. Obtain the source

```text
git clone https://github.com/hosseinTabasi/smart-personal-knowledge-hub.git
```

If you received a ZIP from the course portal, extract it to a folder without spaces if possible, for example `C:\src\smart-personal-knowledge-hub`.

## 3. Open the solution

1. Start Visual Studio 2022.
2. **File → Open → Project/Solution**.
3. Select `SmartKnowledgeHub.sln`.
4. Wait for solution load. If NuGet restore does not start, right-click the solution and choose **Restore NuGet Packages**.

Packages used:

- `CommunityToolkit.Mvvm` (App)
- `Microsoft.Data.Sqlite` (Core and App)
- `Microsoft.Extensions.DependencyInjection` (App)
- `xunit` / `Microsoft.NET.Test.Sdk` (tests)

ONNX Runtime is **not** required. Do not restore large models.

## 4. Set the startup project

1. In Solution Explorer, right-click `SmartKnowledgeHub.App`.
2. **Set as Startup Project**.
3. Configuration: **Debug | Any CPU**.

## 5. Run

Press **F5** (Debug) or **Ctrl+F5** (without debugger).

Expected first-run behaviour:

- Window title: Smart Personal Knowledge Hub
- Left rail: Dashboard, Notes, Tasks, Files, Search, Settings
- Empty dashboard counts until you seed data
- Database created at `%LocalAppData%\SmartKnowledgeHub\hub.db`

## 6. Load sample data

1. Open **Settings**.
2. Click **Seed sample data**.
3. Confirm the message (10 notes, 6 tasks, 3 file records).
4. Return to Dashboard and Search.

Seeding is skipped if notes or tasks already exist, so a second click will not duplicate rows.

## 7. Run tests from Visual Studio

1. **Test → Run All Tests**.
2. Only `SmartKnowledgeHub.Tests` executes. The WPF project is not a test project.
3. Alternatively, **View → Terminal** and run:

```text
dotnet test tests\SmartKnowledgeHub.Tests\SmartKnowledgeHub.Tests.csproj
```

## 8. Common problems

| Symptom | What to try |
| --- | --- |
| Project will not load (`net8.0-windows`) | Install the .NET 8 desktop workload; retargeting to .NET Framework is not supported. |
| NuGet restore fails | Check campus proxy; restore from Visual Studio with the official nuget.org source. |
| SQLite locked | Close a second instance of the app or an external DB browser. |
| Search returns nothing | Seed sample data, then Search → Rebuild index. |
| Optional ONNX message | Normal. The file `minilm.onnx` is not shipped. |

## 9. Where files live after F5

Visual Studio writes binaries under `src\SmartKnowledgeHub.App\bin\Debug\net8.0-windows\`. The **user data** is not in that folder; it is under LocalAppData as listed in the README.
