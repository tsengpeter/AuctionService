FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install PostgreSQL client libraries
RUN apt-get update && apt-get install -y \
    libpq-dev \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/MemberService.API/MemberService.API.csproj", "src/MemberService.API/"]
RUN dotnet restore "src/MemberService.API/MemberService.API.csproj"
COPY . .
WORKDIR "/src/src/MemberService.API"
RUN dotnet build "MemberService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MemberService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MemberService.API.dll"]