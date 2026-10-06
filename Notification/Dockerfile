FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Domain/Domain.csproj Domain/
COPY Application/Application.csproj Application/
COPY Notification/Notification.csproj Notification/
RUN dotnet restore Notification/Notification.csproj
COPY Domain/ Domain/
COPY Application/ Application/
COPY Notification/ Notification/
RUN dotnet publish Notification/Notification.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Notification.dll"]
