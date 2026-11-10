# Build context: repository root.  docker build -f src/Services/Cashflow/Treasury.Cashflow.Api/Dockerfile -t treasury/cashflow-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY Directory.Build.props global.json .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Services/Cashflow/Treasury.Cashflow.Api -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Treasury.Cashflow.Api.dll"]
