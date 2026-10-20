# modular-platform

## Build & Tests
dotnet clean; dotnet build; dotnet test --no-build

## Clean
```bash
dotnet clean
```
```powershel
Get-ChildItem -Directory -Recurse -Force |
     Where-Object { $_.Name -in @('bin', 'obj', '.vs', 'TestResults') } |
     Remove-Item -Recurse -Force
```