FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["FinancialWebApplication/FinancialWebApplication.csproj", "FinancialWebApplication/"]
RUN dotnet restore "FinancialWebApplication/FinancialWebApplication.csproj"

COPY . .
WORKDIR "/src/FinancialWebApplication"
RUN dotnet build "FinancialWebApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FinancialWebApplication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Устанавливаем Production окружение
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://*:80

ENTRYPOINT ["dotnet", "FinancialWebApplication.dll"]