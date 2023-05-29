FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["FPype.Test/FPype.Test.fsproj", "FPype.Test/"]
RUN dotnet restore "FPype.Test/FPype.Test.fsproj"
COPY . .
WORKDIR "/src/FPype.Test"
RUN dotnet build "FPype.Test.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FPype.Test.fsproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FPype.Test.dll"]
