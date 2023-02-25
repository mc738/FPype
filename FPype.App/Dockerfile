FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["FPype.App/FPype.App.fsproj", "FPype.App/"]
RUN dotnet restore "FPype.App/FPype.App.fsproj"
COPY . .
WORKDIR "/src/FPype.App"
RUN dotnet build "FPype.App.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FPype.App.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FPype.App.dll"]
