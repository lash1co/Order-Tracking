FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY NuGet.Config ./
COPY Directory.Build.props ./
COPY global.json ./
COPY OrderTracking.sln ./
COPY src/OrderTracking.Domain/OrderTracking.Domain.csproj src/OrderTracking.Domain/
COPY src/OrderTracking.Application/OrderTracking.Application.csproj src/OrderTracking.Application/
COPY src/OrderTracking.Infrastructure/OrderTracking.Infrastructure.csproj src/OrderTracking.Infrastructure/
COPY src/OrderTracking.API/OrderTracking.API.csproj src/OrderTracking.API/
RUN dotnet restore src/OrderTracking.API/OrderTracking.API.csproj

FROM restore AS build
COPY src src
RUN dotnet publish src/OrderTracking.API/OrderTracking.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "OrderTracking.API.dll"]
