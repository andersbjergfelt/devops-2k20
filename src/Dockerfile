FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY WebApplication/WebApplication.csproj WebApplication/

RUN dotnet restore "WebApplication/WebApplication.csproj"
COPY . .
WORKDIR "/src/WebApplication"
RUN dotnet build "WebApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApplication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /src
COPY wait-for-it.sh /app/wait-for-it.sh
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT [ "./wait-for-it.sh", "db:1433", "-t", "0", "--", "dotnet", "WebApplication.dll"]

