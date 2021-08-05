#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["BOMjak.Bot/BOMjak.Bot.csproj", "BOMjak.Bot/"]
COPY ["BOMjak.Core/BOMjak.Core.csproj", "BOMjak.Core/"]
RUN dotnet restore "BOMjak.Bot/BOMjak.Bot.csproj"
COPY . .
WORKDIR "/src/BOMjak.Bot"
RUN dotnet build "BOMjak.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BOMjak.Bot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BOMjak.Bot.dll"]