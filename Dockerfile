FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CashFlow.sln .
COPY src/CashFlow.Domain/CashFlow.Domain.csproj src/CashFlow.Domain/
COPY src/CashFlow.Application/CashFlow.Application.csproj src/CashFlow.Application/
COPY src/CashFlow.Infrastructure/CashFlow.Infrastructure.csproj src/CashFlow.Infrastructure/
COPY src/CashFlow.Api/CashFlow.Api.csproj src/CashFlow.Api/
COPY tests/CashFlow.UnitTests/CashFlow.UnitTests.csproj tests/CashFlow.UnitTests/
COPY tests/CashFlow.IntegrationTests/CashFlow.IntegrationTests.csproj tests/CashFlow.IntegrationTests/

RUN dotnet restore CashFlow.sln

COPY . .

RUN dotnet publish src/CashFlow.Api/CashFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN addgroup --system app && adduser --system --ingroup app app
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CashFlow.Api.dll"]
