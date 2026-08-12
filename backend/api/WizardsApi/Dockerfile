FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, on csproj files only, so dependency layers cache across source edits.
COPY Wizards.Domain/Wizards.Domain.csproj Wizards.Domain/
COPY Wizards.Application/Wizards.Application.csproj Wizards.Application/
COPY Wizards.Infrastructure/Wizards.Infrastructure.csproj Wizards.Infrastructure/
COPY Wizards.Api/Wizards.Api.csproj Wizards.Api/
RUN dotnet restore Wizards.Api/Wizards.Api.csproj

COPY . .
RUN dotnet publish Wizards.Api/Wizards.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# SQLite lives here. Container-local on purpose: the database dies with the container.
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "Wizards.Api.dll"]
