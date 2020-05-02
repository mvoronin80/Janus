FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

# Copy csproj and restore as distinct layers
COPY ./Janus.Common/ ./Janus.Common
COPY ./Janus.Outside/ ./Janus.Outside

# Copy everything else and build
WORKDIR /Janus.Outside
RUN dotnet publish -c Release -o out

# export port for app
EXPOSE $PORT

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /Janus.Outside
COPY --from=build-env /Janus.Outside/out .
ENTRYPOINT ["dotnet", "Janus.Outside.dll"]

