FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json ./
COPY BookShoppingCart.WebAPI/BookShoppingCart.WebAPI.csproj BookShoppingCart.WebAPI/
COPY BookShoppingCart.Business/BookShoppingCart.Business.csproj BookShoppingCart.Business/
COPY BookShoppingCart.Data/BookShoppingCart.Data.csproj BookShoppingCart.Data/
COPY BookShoppingCart.Models/BookShoppingCart.Models.csproj BookShoppingCart.Models/
COPY BookShoppingCartMvcCore/BookStoreCore.csproj BookShoppingCartMvcCore/
RUN dotnet restore BookShoppingCart.WebAPI/BookShoppingCart.WebAPI.csproj
COPY . .
RUN dotnet publish BookShoppingCart.WebAPI/BookShoppingCart.WebAPI.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookShoppingCart.WebAPI.dll"]
