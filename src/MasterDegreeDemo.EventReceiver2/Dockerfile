FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MasterDegreeDemo.EventReceiver2/MasterDegreeDemo.EventReceiver2.csproj", "MasterDegreeDemo.EventReceiver2/"]
COPY ["MasterDegreeDemo.ServiceDefaults/MasterDegreeDemo.ServiceDefaults.csproj", "MasterDegreeDemo.ServiceDefaults/"]
RUN dotnet restore "./MasterDegreeDemo.EventReceiver2/MasterDegreeDemo.EventReceiver2.csproj"
COPY . .
WORKDIR "/src/MasterDegreeDemo.EventReceiver2"
RUN dotnet build "./MasterDegreeDemo.EventReceiver2.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MasterDegreeDemo.EventReceiver2.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MasterDegreeDemo.EventReceiver2.dll"]