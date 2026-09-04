FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["VpsMonitor.Web.csproj", "./"]
RUN dotnet restore "VpsMonitor.Web.csproj"
COPY . .
RUN dotnet publish "VpsMonitor.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 5080
ENV ASPNETCORE_HTTP_PORTS=5080
ENV ASPNETCORE_URLS=http://0.0.0.0:5080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VpsMonitor.Web.dll"]
