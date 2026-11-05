# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RaceDay.Contracts/RaceDay.Contracts.csproj RaceDay.Contracts/
COPY RaceDay.Mvc/RaceDay.Mvc.csproj RaceDay.Mvc/
RUN dotnet restore RaceDay.Mvc/RaceDay.Mvc.csproj

COPY RaceDay.Contracts/ RaceDay.Contracts/
COPY RaceDay.Mvc/ RaceDay.Mvc/
RUN dotnet publish RaceDay.Mvc/RaceDay.Mvc.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]