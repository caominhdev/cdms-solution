FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY cdms-solution.sln ./
COPY src/CDMS.Core/CDMS.Core.csproj src/CDMS.Core/
COPY src/CDMS.Infrastructure/CDMS.Infrastructure.csproj src/CDMS.Infrastructure/
COPY mock-services/VietfulMockService/VietfulMockService.csproj mock-services/VietfulMockService/

RUN dotnet restore mock-services/VietfulMockService/VietfulMockService.csproj

COPY src/CDMS.Core/ src/CDMS.Core/
COPY src/CDMS.Infrastructure/ src/CDMS.Infrastructure/
COPY mock-services/VietfulMockService/ mock-services/VietfulMockService/

WORKDIR /app/mock-services/VietfulMockService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5100
ENTRYPOINT ["dotnet", "VietfulMockService.dll"]
