FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the entire project first to retain ABP project structure during restore
COPY . .

WORKDIR "/src/LTC.PaymentService.HttpApi.Host"
RUN dotnet restore "./LTC.PaymentService.HttpApi.Host.csproj"
RUN dotnet publish "./LTC.PaymentService.HttpApi.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LTC.PaymentService.HttpApi.Host.dll"]