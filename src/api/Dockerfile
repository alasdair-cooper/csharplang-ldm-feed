FROM mcr.microsoft.com/dotnet/sdk:10.0.100-rc.1 AS build
ARG BUILD_CONFIGURATION=Release
COPY src/api/AlasdairCooper.CSharpLangLdmFeed.Api.csproj api/
COPY src/service-defaults/AlasdairCooper.CSharpLangLdmFeed.ServiceDefaults.csproj service-defaults/
WORKDIR /api
RUN dotnet restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY src/api/ api/
COPY src/service-defaults/ service-defaults/
WORKDIR /api
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-rc.1 AS base
WORKDIR /api
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlasdairCooper.CSharpLangLdmFeed.Api.dll"]


