# Используем официальный образ .NET SDK 10.0 для сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DutyApp.API.csproj", "./"]
RUN dotnet restore "DutyApp.API.csproj"
COPY . .
RUN dotnet publish "DutyApp.API.csproj" -c Release -o /app/publish

# Используем официальный образ ASP.NET Core Runtime для запуска
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DutyApp.API.dll"]