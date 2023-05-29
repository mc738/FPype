FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["FPype.Service/FPype.Service.fsproj", "FPype.Service/"]
RUN dotnet restore "FPype.Service/FPype.Service.fsproj"
COPY . .
WORKDIR "/src/FPype.Service"
RUN dotnet build "FPype.Service.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FPype.Service.fsproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FPype.Service.dll"]
