dotnet ef migrations add InitialCreate \
--project src/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj \
--startup-project src/Demo.Api/Demo.Api.csproj \
--output-dir Migrations

dotnet ef migrations add InitialCreate --project src/Applications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj --startup-project src/Applications/Demo.Api/Demo.Api.csproj --output-dir Migrations

dotnet ef database update --project src/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj --startup-project src/Demo.Api/Demo.Api.csproj


dotnet ef migrations add InitialCreate --project src/DemoApplications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj --startup-project src/DemoApplications/Demo.Api/Demo.Api.csproj
dotnet ef database update --project src/DemoApplications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj --startup-project src/DemoApplications/Demo.Api/Demo.Api.csproj