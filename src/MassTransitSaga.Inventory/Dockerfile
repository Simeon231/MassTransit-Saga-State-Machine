FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MassTransitSaga.Inventory/MassTransitSaga.Inventory.csproj", "MassTransitSaga.Inventory/"]
COPY ["MassTransitSaga.Shared/MassTransitSaga.Shared.csproj", "MassTransitSaga.Shared/"]
RUN dotnet restore "./MassTransitSaga.Inventory/MassTransitSaga.Inventory.csproj"
COPY . .
WORKDIR "/src/MassTransitSaga.Inventory"
RUN dotnet build "./MassTransitSaga.Inventory.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MassTransitSaga.Inventory.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MassTransitSaga.Inventory.dll"]