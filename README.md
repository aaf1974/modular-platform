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

## Project to fiile
```powershel
$sourcePath = "E:\MyDevWork\modular-platform\src\Platform.Core\"
$outputFile = "E:\MyDevWork\Platform.txt"

Remove-Item $outputFile -ErrorAction SilentlyContinue

Get-ChildItem -Path $sourcePath -Recurse -Filter *.cs | ForEach-Object {
    Add-Content $outputFile "`r`n===== FILE: $($_.FullName) =====`r`n"
    Get-Content $_.FullName | Add-Content $outputFile
}
```

## Release
```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```