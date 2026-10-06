FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Analytics/Analytics.csproj Analytics/
RUN dotnet restore Analytics/Analytics.csproj
COPY Analytics/ Analytics/
RUN dotnet publish Analytics/Analytics.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Analytics.dll"]
