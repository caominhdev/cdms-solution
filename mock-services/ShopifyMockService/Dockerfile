FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY cdms-solution.sln ./
COPY src/CDMS.Core/CDMS.Core.csproj src/CDMS.Core/
COPY mock-services/ShopifyMockService/ShopifyMockService.csproj mock-services/ShopifyMockService/

RUN dotnet restore mock-services/ShopifyMockService/ShopifyMockService.csproj

COPY src/CDMS.Core/ src/CDMS.Core/
COPY mock-services/ShopifyMockService/ mock-services/ShopifyMockService/

WORKDIR /app/mock-services/ShopifyMockService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5200
ENTRYPOINT ["dotnet", "ShopifyMockService.dll"]
