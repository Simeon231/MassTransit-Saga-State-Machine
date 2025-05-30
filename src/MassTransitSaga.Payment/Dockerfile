FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MassTransitSaga.Payment/MassTransitSaga.Payment.csproj", "MassTransitSaga.Payment/"]
COPY ["MassTransitSaga.Shared/MassTransitSaga.Shared.csproj", "MassTransitSaga.Shared/"]
RUN dotnet restore "./MassTransitSaga.Payment/MassTransitSaga.Payment.csproj"
COPY . .
WORKDIR "/src/MassTransitSaga.Payment"
RUN dotnet build "./MassTransitSaga.Payment.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MassTransitSaga.Payment.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MassTransitSaga.Payment.dll"]