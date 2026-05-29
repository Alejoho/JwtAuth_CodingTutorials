# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BlazorWasmAuthentication/BlazorWasmAuthentication.csproj", "BlazorWasmAuthentication/"]
RUN dotnet restore "./BlazorWasmAuthentication/BlazorWasmAuthentication.csproj"
COPY . .
WORKDIR "/src/BlazorWasmAuthentication"
# RUN dotnet build "./BlazorWasmAuthentication.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BlazorWasmAuthentication.csproj" -c $BUILD_CONFIGURATION -o /app/publish


FROM nginx:alpine AS final

EXPOSE 80

# Copy the custom Nginx configuration file into the container
COPY default.conf /etc/nginx/conf.d/default.conf

COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html