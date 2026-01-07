FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:9000
EXPOSE 9000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy csproj and restore (context is ./Biletado in the workflow)
COPY ["Biletado.csproj", "./"]
RUN dotnet restore "Biletado.csproj"

# Copy the rest and build
COPY . .
WORKDIR "/src"
RUN dotnet build "Biletado.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Biletado.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Biletado.dll"]
