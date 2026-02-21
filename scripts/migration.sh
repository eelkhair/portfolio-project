#!/bin/bash
cd ../services/micro-services/company-api/CompanyAPI.Service
echo "Current directory: $(pwd)"
dotnet ef database update

cd ../../user-api/UserApi.Service
echo "Current directory: $(pwd)"
dotnet ef database update

cd ../../job-api/JobApi.Service
echo "Current directory: $(pwd)"
dotnet ef database update

cd ../../../monolith
echo "Current directory: $(pwd)"
dotnet ef database update --project Src/Infrastructure/JobBoard.Infrastructure.Persistence/JobBoard.Infrastructure.Persistence.csproj --startup-project Src/Presentation/JobBoard.API/JobBoard.API.csproj --context JobBoard.Infrastructure.Persistence.Context.JobBoardDbContext