FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OnlineStore/OnlineStore.csproj", "OnlineStore/"]
COPY ["OnlineStore.Models/OnlineStore.Models.csproj", "OnlineStore.Models/"]
COPY ["OnlineStore.DataAccess/OnlineStore.DataAccess.csproj", "OnlineStore.DataAccess/"]
COPY ["OnlineStore.Business/OnlineStore.Business.csproj", "OnlineStore.Business/"]
RUN dotnet restore "OnlineStore/OnlineStore.csproj"
COPY . .
WORKDIR /src/OnlineStore
RUN dotnet publish "OnlineStore.csproj" -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R app:app /app
USER app
ENV ASPNETCORE_ENVIRONMENT=Production
CMD ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet OnlineStore.dll
