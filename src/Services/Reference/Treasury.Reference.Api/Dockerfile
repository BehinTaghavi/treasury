# Build context: repository root.  docker build -f src/Services/Reference/Treasury.Reference.Api/Dockerfile -t treasury/reference-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY Directory.Build.props global.json .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Services/Reference/Treasury.Reference.Api -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Treasury.Reference.Api.dll"]
