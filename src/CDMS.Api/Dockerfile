FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY cdms-solution.sln ./
COPY src/CDMS.Core/CDMS.Core.csproj src/CDMS.Core/
COPY src/CDMS.Infrastructure/CDMS.Infrastructure.csproj src/CDMS.Infrastructure/
COPY src/CDMS.Connectors.Vietful/CDMS.Connectors.Vietful.csproj src/CDMS.Connectors.Vietful/
COPY src/CDMS.Connectors.Shopify/CDMS.Connectors.Shopify.csproj src/CDMS.Connectors.Shopify/
COPY src/CDMS.Metrics/CDMS.Metrics.csproj src/CDMS.Metrics/
COPY src/CDMS.Workers/CDMS.Workers.csproj src/CDMS.Workers/
COPY src/CDMS.Api/CDMS.Api.csproj src/CDMS.Api/
COPY src/CDMS.Tests/CDMS.Tests.csproj src/CDMS.Tests/
COPY mock-services/VietfulMockService/VietfulMockService.csproj mock-services/VietfulMockService/
COPY mock-services/ShopifyMockService/ShopifyMockService.csproj mock-services/ShopifyMockService/

RUN dotnet restore src/CDMS.Api/CDMS.Api.csproj

# Copy everything else and build
COPY src/ src/
WORKDIR /app/src/CDMS.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "CDMS.Api.dll"]
