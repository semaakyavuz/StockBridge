# ---- Build asamasi ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY StockBridge.API.csproj .
RUN dotnet restore StockBridge.API.csproj

COPY . .
RUN dotnet publish StockBridge.API.csproj -c Release -o /app/publish --no-restore

# ---- Calisma zamani asamasi ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN adduser --disabled-password --home /app --gecos '' appuser \
    && chown -R appuser /app
USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production
# Railway PORT env degiskenini kendisi enjekte eder ve Program.cs bunu okuyup
# Kestrel'i buna gore baglar; EXPOSE burada sadece dokumantasyon amaclidir.
EXPOSE 8080

ENTRYPOINT ["dotnet", "StockBridge.API.dll"]
