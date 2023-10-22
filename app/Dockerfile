# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH

RUN dotnet tool install -g csharprepl

# code-server needs nodejs, npm, python
#RUN apk add --update nodejs npm
#RUN apk add --no-cache python3 py3-pip

# install VS Code (code-server)
#RUN curl -fsSL https://code-server.dev/install.sh | sh

# install VS Code extensions
#RUN code-server --install-extension ms-dotnettools.csharp

WORKDIR /source/app

# Docker container needs at-least 1 running process, or it will exit
CMD ["sleep", "infinity"] 