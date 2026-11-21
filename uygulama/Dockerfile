FROM mcr.microsoft.com/dotnet/sdk:8.0 AS derleme
WORKDIR /kaynak
COPY Yemekhane.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /yayin --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=derleme /yayin ./
ENV ASPNETCORE_HTTP_PORTS=8000
EXPOSE 8000
ENTRYPOINT ["dotnet", "Yemekhane.dll"]