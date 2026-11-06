# ---------- Base runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8000

ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY BookShoppingCartMvcUI/BookShoppingCartMvcUI.csproj BookShoppingCartMvcUI/
COPY BookShoppingCart.Business/BookShoppingCart.Business.csproj BookShoppingCart.Business/
COPY BookShoppingCart.Data/BookShoppingCart.Data.csproj BookShoppingCart.Data/
COPY BookShoppingCart.Models/BookShoppingCart.Models.csproj BookShoppingCart.Models/
COPY BookShoppingCartMvcCore/BookStoreCore.csproj BookShoppingCartMvcCore/
RUN dotnet restore BookShoppingCartMvcUI/BookShoppingCartMvcUI.csproj

COPY . .
WORKDIR /src/BookShoppingCartMvcUI

# ---------- Publish ----------
FROM build AS publish
RUN dotnet publish BookShoppingCartMvcUI.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---------- Final ---------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookShoppingCartMvcUI.dll"]
