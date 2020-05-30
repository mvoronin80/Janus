FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

# Copy Common and Outside project to container
COPY ./Janus.Common/ ./Janus.Common
COPY ./Janus.Outside/ ./Janus.Outside

# set working dir and build release version of Janus.Outside
WORKDIR /Janus.Outside
RUN dotnet publish -c Release -o out

# export port for app
EXPOSE $PORT

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /Janus.Outside
COPY --from=build-env /Janus.Outside/out .
CMD ASPNETCORE_URLS=http://*:$PORT dotnet Janus.Outside.dll

