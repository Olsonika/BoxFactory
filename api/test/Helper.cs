﻿using Dapper;
using Newtonsoft.Json;
using Npgsql;
using NUnit.Framework;

namespace test;

public class Helper
{
    public static readonly Uri Uri;
    private static readonly string ProperlyFormattedConnectionString;
    public static readonly NpgsqlDataSource DataSource;

    static Helper()
    {
        string rawConnectionString;
        string envVarKeyName = "pgconn";

        rawConnectionString = Environment.GetEnvironmentVariable(envVarKeyName)!;
        if (rawConnectionString == null)
        {
            throw new Exception($@"The Connection String is empty.");
        }

        try
        {
            Uri = new Uri(rawConnectionString);
            ProperlyFormattedConnectionString = string.Format("Server={0};Database={1};User Id={2};Password={3};Port={4};Pooling=true;MaxPoolSize=3",
                Uri.Host,
                Uri.AbsolutePath.Trim('/'),
                Uri.UserInfo.Split(':')[0],
                Uri.UserInfo.Split(':')[1],
                Uri.Port > 0 ? Uri.Port : 5432);
            DataSource =
                new NpgsqlDataSourceBuilder(ProperlyFormattedConnectionString).Build();
            DataSource.OpenConnection().Close();
        }
        catch ( Exception e)
        {
            throw new Exception($@"Connection string found but there was an issue. The input may be incorrect.");
        }
    }

    public static string BadResponseBody(string content)
    {
        return $@"There was an issue fetching the response body from the API and turning it into a class object.
        Reponse Body: {content}

        EXCEPTION:
        ";
    }

    public static void TriggerRebuild()
    {
        using (var conn = DataSource.OpenConnection())
        {
            try
            {
                conn.Execute(RebuildScript);
            }
            catch (Exception e)
            {
                throw new Exception($@"
                There was an error rebuilding the DB.
                EXCEPTION: 
                ", e);
            }
        }
    }

    public static string RebuildScript = @"
DROP SCHEMA IF EXISTS box_factory CASCADE;
CREATE SCHEMA box_factory;

create table if not exists box_factory.boxes
(
    id       integer generated by default as identity,
    size     text,
    weight   float,
    price    float,
    material text,
    color    text,
    quantity integer,
    constraint boxespk primary key (id)
);";

    public static string NoResponseMessage = $@"
There was no response from the API, the API may not be running.
    ";
    
    public static async Task<bool> IsCorsFullyEnabledAsync(string path)
    {
        using var client = new HttpClient();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Options, new Uri(path));
            // Add Origin header to simulate CORS request
            request.Headers.Add("Origin", "https://week35-86108.web.app");
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "X-Requested-With");

            var response = await client.SendAsync(request);

            bool corsEnabled = false;

            if (response.Headers.Contains("Access-Control-Allow-Origin"))
            {
                var accessControlAllowOrigin =
                    response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
                corsEnabled = accessControlAllowOrigin == "*" ||
                              accessControlAllowOrigin == "https://week35-86108.web.app";
            }

            var accessControlAllowMethods = response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault();
            var accessControlAllowHeaders = response.Headers.GetValues("Access-Control-Allow-Headers").FirstOrDefault();

            if (corsEnabled && (accessControlAllowMethods != null && accessControlAllowMethods.Contains("GET")) &&
                (accessControlAllowHeaders != null && accessControlAllowHeaders.Contains("X-Requested-With")))
            {
                return true;
            }
        }
        catch (Exception)
        {
            throw new Exception("\nCORS IS NOT ENABLED. PLEASE ENABLE CORS.\n(check last part of the project description)\n");
        }


        return false;
    }
    
    public static string MyBecause(object actual, object expected)
    {
        string expectedJson = JsonConvert.SerializeObject(expected, Formatting.Indented);
        string actualJson = JsonConvert.SerializeObject(actual, Formatting.Indented);

        return $"because we want these objects to be equivalent:\nExpected:\n{expectedJson}\nActual:\n{actualJson}";
    }
}