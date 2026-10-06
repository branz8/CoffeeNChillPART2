FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY . ./
RUN dotnet restore "CoffeeNChill.Functions.csproj"
RUN dotnet publish "CoffeeNChill.Functions.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
	DOTNET_USE_POLLING_FILE_WATCHER=1

ENTRYPOINT ["/azure-functions-host/Microsoft.Azure.Functions.Worker.Host", "--host", "0.0.0.0"]
