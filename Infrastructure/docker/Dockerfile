FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["../feat.api.csproj", "./"]
RUN dotnet restore "./feat.api.csproj"
COPY .. .
WORKDIR /src/
RUN dotnet build "./feat.api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN mkdir /app/https
RUN dotnet publish "./feat.api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
RUN dotnet dev-certs https -ep /app/https/aspnetapp.pfx -p e2e

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/https /https
ENTRYPOINT ["dotnet", "feat.api.dll"]
EXPOSE 8080
EXPOSE 8081
