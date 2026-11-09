FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/SchoolAdmission.Api/SchoolAdmission.Api.csproj src/SchoolAdmission.Api/
RUN dotnet restore src/SchoolAdmission.Api/SchoolAdmission.Api.csproj

COPY src/SchoolAdmission.Api/ src/SchoolAdmission.Api/
RUN dotnet publish src/SchoolAdmission.Api/SchoolAdmission.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
