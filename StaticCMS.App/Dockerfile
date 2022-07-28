FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["StaticCMS.App/StaticCMS.App.fsproj", "StaticCMS.App/"]
RUN dotnet restore "StaticCMS.App/StaticCMS.App.fsproj"
COPY . .
WORKDIR "/src/StaticCMS.App"
RUN dotnet build "StaticCMS.App.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StaticCMS.App.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StaticCMS.App.dll"]
