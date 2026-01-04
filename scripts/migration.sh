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