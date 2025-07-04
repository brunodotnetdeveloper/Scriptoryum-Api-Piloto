# ...existing code...

# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Ajuste o caminho conforme a estrutura real do projeto.
# Se o build está sendo executado a partir da raiz, mantenha assim:
COPY Scriptoryum.Api/Scriptoryum.Api.csproj Scriptoryum.Api/
# ...restante do Dockerfile...

# ...existing code...