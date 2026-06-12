FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY src/EHealth.Patient.Api/EHealth.Patient.Api.csproj EHealth.Patient.Api/
RUN dotnet restore EHealth.Patient.Api/EHealth.Patient.Api.csproj -a $TARGETARCH

COPY src/EHealth.Patient.Api/ EHealth.Patient.Api/
RUN dotnet publish EHealth.Patient.Api/EHealth.Patient.Api.csproj \
    -c Release -o /out --no-restore -a $TARGETARCH

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
EXPOSE 3001
ENV ASPNETCORE_URLS=http://+:3001
ENTRYPOINT ["dotnet", "EHealth.Patient.Api.dll"]
