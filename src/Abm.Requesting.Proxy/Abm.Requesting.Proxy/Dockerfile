FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Abm.Requesting.Proxy/Abm.Requesting.Proxy.csproj", "Abm.Requesting.Proxy/"]
RUN dotnet restore "Abm.Requesting.Proxy/Abm.Requesting.Proxy.csproj"
COPY . .
WORKDIR "/src/Abm.Requesting.Proxy"
RUN dotnet build "Abm.Requesting.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Abm.Requesting.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Abm.Requesting.Proxy.dll"]
