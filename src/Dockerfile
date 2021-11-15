# Base image used by Visual Studio at development time
# (See https://docs.microsoft.com/en-us/visualstudio/containers/container-msbuild-properties)
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
# Disable the culture invariant mode which defaults to true in the base alpine image
# (See https://github.com/dotnet/corefx/blob/8245ee1e8f6063ccc7a3a60cafe821d29e85b02f/Documentation/architecture/globalization-invariant-mode.md)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache \
    # Install cultures to enable use of System.CultureInfo
    icu-libs \
    # Install time zone database to enable use of System.TimeZoneInfo
    tzdata
# Set the default locale and language.
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8
WORKDIR /app
EXPOSE 80 443

# SDK image used to build and publish the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS sdk
# To use the debug build configuration pass --build-arg Configuration=Debug
ARG Configuration=Release
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
WORKDIR /src
COPY ["src/YA.ServiceTemplate.csproj", "src/"]
RUN dotnet restore "src/YA.ServiceTemplate.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "YA.ServiceTemplate.csproj" -c $Configuration -o /app/build

FROM build AS publish
RUN dotnet publish "YA.ServiceTemplate.csproj" -c $Configuration -o /app/publish

# Runtime image used to run the application
FROM base AS runtime
WORKDIR /app
COPY --from=sdk /app/publish .
ENTRYPOINT ["dotnet", "YA.ServiceTemplate.dll"]
