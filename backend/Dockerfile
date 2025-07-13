FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CvAnalysis.Server.csproj", "./"]
RUN dotnet restore "CvAnalysis.Server.csproj"
COPY . .
RUN dotnet publish "CvAnalysis.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CvAnalysis.Server.dll"]
