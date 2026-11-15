# Build context: repository root.  docker build -f src/Services/Accounts/Treasury.Accounts.Api/Dockerfile -t treasury/accounts-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY Directory.Build.props global.json .editorconfig ./
COPY src/ src/
# NuGet packages go to a BuildKit cache that outlives the build: a retry after a broken download (slow line)
# continues instead of starting again, and the API images share one download.
RUN --mount=type=cache,id=treasury-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish src/Services/Accounts/Treasury.Accounts.Api -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Treasury.Accounts.Api.dll"]
