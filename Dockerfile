FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV SimpleProperty="hello-from-BASE-dockerfile"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KoiGuardian.Api/KoiGuardian.Api.csproj", "KoiGuardian.Api/"]
COPY ["KoiGuardian.Core/KoiGuardian.Core.csproj", "KoiGuardian.Core/"]
COPY ["KoiGuardian.DataAccess/KoiGuardian.DataAccess.csproj", "KoiGuardian.DataAccess/"]
COPY ["KoiGuardian.Models/KoiGuardian.Models.csproj", "KoiGuardian.Models/"]
RUN dotnet restore "KoiGuardian.Api/KoiGuardian.Api.csproj"
COPY . .
WORKDIR "/src/KoiGuardian.Api"
RUN dotnet build "KoiGuardian.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KoiGuardian.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV SimpleProperty="hello-from-FINAL-dockerfile"
ENTRYPOINT ["dotnet", "KoiGuardian.Api.dll"]
