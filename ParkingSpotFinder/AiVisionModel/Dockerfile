FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AiVisionModel/AiVisionModel.csproj", "AiVisionModel/"]
RUN dotnet restore "AiVisionModel/AiVisionModel.csproj"
COPY . .
WORKDIR "/src/AiVisionModel"
RUN dotnet build "AiVisionModel.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AiVisionModel.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiVisionModel.dll"]