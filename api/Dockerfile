FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["api.csproj", "./"]
RUN dotnet restore "./api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "api.csproj" -c Release -o /app/build

FROM build AS publish
# Build as self-contained for Linux
RUN dotnet publish "api.csproj" \
    -c Release \
    -o /app/publish \
    -r linux-x64 \
    --self-contained true \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["./api"]
