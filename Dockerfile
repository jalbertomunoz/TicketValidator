FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src

COPY TicketValidator.sln ./
COPY src/TicketValidator.Domain/TicketValidator.Domain.csproj src/TicketValidator.Domain/
COPY src/TicketValidator.Application/TicketValidator.Application.csproj src/TicketValidator.Application/
COPY src/TicketValidator.Infrastructure/TicketValidator.Infrastructure.csproj src/TicketValidator.Infrastructure/
COPY src/TicketValidator.Api/TicketValidator.Api.csproj src/TicketValidator.Api/

RUN dotnet restore src/TicketValidator.Api/TicketValidator.Api.csproj --runtime linux-x64

COPY src/ src/

RUN dotnet publish src/TicketValidator.Api/TicketValidator.Api.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained false \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS native-build
ARG LEPTONICA_VERSION=1.85.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        cmake \
        curl \
        g++ \
        libjpeg-dev \
        libpng-dev \
        make \
        zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /tmp
RUN curl --fail --location --silent --show-error \
        "https://github.com/DanBloomberg/leptonica/archive/refs/tags/${LEPTONICA_VERSION}.tar.gz" \
        --output leptonica.tar.gz \
    && tar --extract --gzip --file leptonica.tar.gz \
    && cmake -S "leptonica-${LEPTONICA_VERSION}" -B leptonica-build \
        -DBUILD_SHARED_LIBS=ON \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_INSTALL_PREFIX=/opt/leptonica \
    && cmake --build leptonica-build --parallel "$(nproc)" \
    && cmake --install leptonica-build \
    && test -f /opt/leptonica/lib/libleptonica.so

FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        libjpeg62-turbo \
        libpng16-16 \
        libtesseract5 \
        zlib1g \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish/ ./
COPY --from=native-build /opt/leptonica/lib/ /usr/local/lib/

RUN ldconfig \
    && test -f /app/tessdata/spa.traineddata \
    && test -f /app/tessdata/osd.traineddata \
    && test -f /usr/local/lib/libleptonica.so \
    && test -f /usr/lib/x86_64-linux-gnu/libtesseract.so.5 \
    && ln -s /lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/libdl.so \
    && ldconfig \
    && ln -s /usr/local/lib/libleptonica.so /app/x64/libleptonica-1.85.0.dll.so \
    && ln -s /usr/local/lib/libleptonica.so /app/libleptonica-1.85.0.dll.so \
    && ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract55.dll.so \
    && ! ldd /app/libleptonica-1.85.0.dll.so | grep -q "not found" \
    && ! ldd /app/x64/libleptonica-1.85.0.dll.so | grep -q "not found" \
    && ! ldd /app/x64/libtesseract55.dll.so | grep -q "not found" \
    && mkdir /app/logs \
    && chown -R "$APP_UID:$APP_UID" /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "TicketValidator.Api.dll"]
