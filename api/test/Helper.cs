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
    public static readonly string ClientAppBaseUrl = "http://localhost:4200";

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
    

    

    
    public static string MyBecause(object actual, object expected)
    {
        string expectedJson = JsonConvert.SerializeObject(expected, Formatting.Indented);
        string actualJson = JsonConvert.SerializeObject(actual, Formatting.Indented);

        return $"because we want these objects to be equivalent:\nExpected:\n{expectedJson}\nActual:\n{actualJson}";
    }

}