FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SuperPOS.Shared/SuperPOS.Shared.csproj src/SuperPOS.Shared/
COPY src/SuperPOS.AFIP/SuperPOS.AFIP.csproj src/SuperPOS.AFIP/
COPY src/SuperPOS.API/SuperPOS.API.csproj src/SuperPOS.API/
RUN dotnet restore src/SuperPOS.API/SuperPOS.API.csproj

COPY src/SuperPOS.Shared/ src/SuperPOS.Shared/
COPY src/SuperPOS.AFIP/ src/SuperPOS.AFIP/
COPY src/SuperPOS.API/ src/SuperPOS.API/

RUN dotnet publish src/SuperPOS.API/SuperPOS.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SuperPOS.API.dll"]
