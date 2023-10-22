# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH

RUN dotnet tool install -g csharprepl

WORKDIR /source/app

# Docker container needs at-least 1 running process, or it will exit
CMD ["sleep", "infinity"] 