# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["CinemaBooking.csproj", "./"]
RUN dotnet restore "CinemaBooking.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "CinemaBooking.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "CinemaBooking.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CinemaBooking.dll"]
