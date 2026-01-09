# ---------- Base runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8000

ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BookShoppingCartMvcUI/BookShoppingCartMvcUI.csproj BookShoppingCartMvcUI/
RUN dotnet restore BookShoppingCartMvcUI/BookShoppingCartMvcUI.csproj

COPY . .
WORKDIR /src/BookShoppingCartMvcUI
RUN dotnet build BookShoppingCartMvcUI.csproj -c Release -o /app/build

# ---------- Publish ----------
FROM build AS publish
RUN dotnet publish BookShoppingCartMvcUI.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Final ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookShoppingCartMvcUI.dll"]
