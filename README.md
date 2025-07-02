# api-aggregation

## Overview

**ApiAggregation** is a .NET 9 Web API that aggregates data from multiple external services into a single, consistent response. It demonstrates a Clean Architecture approach with the following layers:

- **Domain**: core entities and value objects
- **Application**: orchestration, and service interfaces
- **Infrastructure**: external API clients, caching, resilience (Polly)
- **WebApi**: ASP .NET Core controllers, DTOs, authentication, middleware

At runtime the API will:

1. Look up a country’s capital via restcountries.com
2. Fetch current weather for that capital from OpenWeatherMap
3. Fetch top headlines for the country from NewsAPI
4. Return all three results in one JSON payload


## Prerequisites
- netcore 9.0
- API keys for:
- **OpenWeatherMap** → store under `ApiKeys:OpenWeatherKey`
- **NewsAPI** → store under `ApiKeys:NewsApiKey`

## Manage Api Keys

Before running the application, you need to set up the environment variables for the API keys.
You can do this using the `dotnet user-secrets` command, which is a secure way to store sensitive information during development.
For our application, you will need to get API keys for [OpenWeatherMap](https://openweathermap.org/api), [NewsAPI](https://newsapi.org/).
[RestCountries](https://restcountries.com/v3.1/all?fields=cca2,name,capital) is not required to set an API key, but it is used to fetch country data.

## Init Secret Variables and JTW Secret key 
Run the following commands in the terminal to set up the user secrets for the application:
```
cd src/ApiAggregation.WebApi

dotnet user-secrets init

dotnet user-secrets set "ApiKeys:OpenWeatherKey"   "<your-weather-key>"
dotnet user-secrets set "ApiKeys:NewsApiKey"          "<your-news-key>"
dotnet user-secrets set "JwtSettings:Username" "ApiAggUsr"
dotnet user-secrets set "JwtSettings:Password" "ApiAggPsw"
SECRET=$(openssl rand -base64 32) && dotnet user-secrets set "JwtSettings:SecretKey" "$SECRET"
```
## Run the tests of the application
Under the /test folder, you will find the test project for the application.
To run the tests, navigate to the test project directory and execute the following command:

```
# Go the root of the repository (where the .sln file is located)
dotnet test ApiAggregation.sln
```

## Running the Application
Set up launchSettings.json file in the src/ApiAggregation.WebApi/Properties folder to run the application. 

```json

{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5087",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7134;http://localhost:5087",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}

```
Then run the following command in the terminal to start the application:

```
dotnet run \                  
  --project src/ApiAggregation.WebApi/ApiAggregation.WebApi.csproj \
  --launch-profile "https"
```

## API Documentation
The API documentation is available at `/swagger/index.html` after running the application. 
It provides detailed information about the endpoints, request/response formats, and authentication requirements.

## API Endpoints

### Authentication

To use the protected endpoints, you first need to authenticate and obtain a JWT token.

#### Generate Authentication Token

```POST /api/auth/token```

**Request Body:**
```json
{
  "userName": "ApiAggUsr",
  "password": "ApiAggPsw"
}
```
**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2023-06-01T12:00:00Z"
}
```
### Aggregated Data API
This endpoint fetches and aggregates data from multiple external APIs including country information, weather data, and news.
Get Aggregated Data

**Authentication Required: Yes (Bearer Token)**

Query Parameters:

- countryName (required): The name of the country to fetch data for
- newsPageSize : Number of news articles to return (default is 10)
- fromDate (required): Start date for news articles in format (yyyy-MM-dd)

### Example Request:
```GET /api/aggregate/getdata?countryName=Greece&newsPageSize=5&fromDate=2025-06-01```

**Response:**
```json
{
    "aggregate": [
        {
            "message": "Success",
            "status": "Success",
            "data": {
                "name": "Greece",
                "capitalCity": "Athens"
            }
        },
        {
            "message": "Success",
            "status": "OK",
            "data": {
                "temperature": 32.66,
                "weather": "few clouds"
            }
        },
        {
            "message": "Success",
            "status": "Success",
            "data": {
                "articles": [
                    {
                        "title": "Euronext statement regarding recent press speculations"
                    },
                    {
                        "title": "Gizchina Greece: Telegram, για άμεση ενημέρωση σε προσφορές!"
                    },
                    {
                        "title": "Migration, Marginalization and Outsider Art Collide in Intuit Art Museum’s ‘Catalyst’"
                    },
                    {
                        "title": "Gerapetritis to The Economist: The EU must step up – Western unity is essential"
                    }
                ]
            }
        }
    ],
    "status": "Success",
    "message": "All data aggregated successfully."
}
```
### Health Check
This endpoint checks the health status of the API.

```GET /api/Aggregate/healthcheck```

**Authentication Required: Yes (Bearer Token)**

**Response:**

```
{
    "status": "Healthy",
    "timestamp": "2023-06-01T12:00:00Z"
}
```