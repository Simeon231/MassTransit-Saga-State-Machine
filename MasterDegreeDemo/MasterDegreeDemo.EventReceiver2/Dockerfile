FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MasterDegreeDemo.EventReceiver1/MasterDegreeDemo.EventReceiver1.csproj", "MasterDegreeDemo.EventReceiver1/"]
COPY ["MasterDegreeDemo.ServiceDefaults/MasterDegreeDemo.ServiceDefaults.csproj", "MasterDegreeDemo.ServiceDefaults/"]
RUN dotnet restore "./MasterDegreeDemo.EventReceiver1/MasterDegreeDemo.EventReceiver1.csproj"
COPY . .
WORKDIR "/src/MasterDegreeDemo.EventReceiver1"
RUN dotnet build "./MasterDegreeDemo.EventReceiver1.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MasterDegreeDemo.EventReceiver1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MasterDegreeDemo.EventReceiver1.dll"]