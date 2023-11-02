# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:6.0-jammy AS build
ARG TARGETARCH

# install VS Code (code-server)
RUN curl -fsSL https://code-server.dev/install.sh | sh

#RUN dotnet dev-certs https --clean
#RUN dotnet dev-certs https -t

# install VS Code extensions
RUN code-server --install-extension ms-dotnettools.csharp

EXPOSE 5000-5001

VOLUME [ "/source/app" ]
WORKDIR /source/app

CMD ["dotnet", "run", "--urls=https://0.0.0.0:5001/", "-c", "Release"] 