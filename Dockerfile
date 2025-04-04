# Use the official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy the project files
COPY . .

# Restore dependencies
RUN dotnet restore

# Build the solution
RUN dotnet build --no-restore

# Run tests and collect coverage
CMD dotnet test --collect:"XPlat Code Coverage" --logger "trx"