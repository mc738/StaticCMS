FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["StaticCMS.WebApi/StaticCMS.WebApi.fsproj", "StaticCMS.WebApi/"]
RUN dotnet restore "StaticCMS.WebApi/StaticCMS.WebApi.fsproj"
COPY . .
WORKDIR "/src/StaticCMS.WebApi"
RUN dotnet build "StaticCMS.WebApi.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StaticCMS.WebApi.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StaticCMS.WebApi.dll"]
