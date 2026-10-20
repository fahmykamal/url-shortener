FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY url-shortener.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "url-shortener.dll"]
