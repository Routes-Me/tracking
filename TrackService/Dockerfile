#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["TrackService/TrackService.csproj", "TrackService/"]
COPY ["TrackService.RethinkDb_Abstractions/TrackService.RethinkDb_Abstractions.csproj", "TrackService.RethinkDb_Abstractions/"]
COPY ["TrackService.RethinkDb_Changefeed/TrackService.RethinkDb_Changefeed.csproj", "TrackService.RethinkDb_Changefeed/"]
RUN dotnet restore "TrackService/TrackService.csproj"
COPY . .
WORKDIR "/src/TrackService"
RUN dotnet build "TrackService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TrackService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrackService.dll"]