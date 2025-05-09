FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["sync.csproj", "./"]
COPY ["../common/common.csproj", "../common/"]
RUN dotnet restore "./sync.csproj"
COPY . .
COPY ["../common/", "../common/"]
WORKDIR "/src/."
RUN dotnet build "sync.csproj" -c Release -o /app/build

FROM build AS publish
# Build as self-contained for Linux
RUN dotnet publish "sync.csproj" \
    -c Release \
    -o /app/publish \
    -r linux-x64 \
    --self-contained true \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./sync"]
