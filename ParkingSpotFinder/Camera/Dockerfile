FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Camera/Camera.csproj", "Camera/"]
RUN dotnet restore "Camera/Camera.csproj"
COPY . .
WORKDIR "/src/Camera"
RUN dotnet build "Camera.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Camera.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Camera.dll"]