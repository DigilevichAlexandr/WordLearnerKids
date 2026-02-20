FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["WordLearnerKids.csproj", "./"]
RUN dotnet restore "WordLearnerKids.csproj"

COPY . .
RUN dotnet publish "WordLearnerKids.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV AppDataRoot=/var/data

EXPOSE 10000

ENTRYPOINT ["dotnet", "WordLearnerKids.dll"]
