﻿using Microsoft.Playwright.NUnit;

namespace test;

using Dapper;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using NUnit.Framework;

public class SearchBox : PageTest
{
    private HttpClient _httpClient;

    [SetUp]
    public void Setup()
    {
        _httpClient = new HttpClient();
    }

    [Test]
    [TestCase("Small")]
    [TestCase("medium")]
    [TestCase("Red")]
    [TestCase("Cardboard")]
    public async Task SuccessfullBoxSearch(string searchterm)
    {
        Helper.TriggerRebuild();
        var expected = new List<object>();
        int expectedResult = 5;
        for (var i = 1; i <= 10; i++)
        {
            var box = new Box()
            {
                Id = i,
                Size = i % 2 == 0 ? "Small" : "Medium",
                Weight = 5,
                Price = 2,
                Material = i % 2 == 0 ? "Cardboard" : "Plastic",
                Color = i % 2 == 0 ? "Red" : "Blue",
                Quantity = 1
            };
            expected.Add(box);

            var sql = $@"INSERT INTO box_factory.boxes (size, weight, price, material, color, quantity)
                    VALUES (@Size, @Weight, @Price, @Material, @Color, @Quantity);
                ";
            using (var conn = Helper.DataSource.OpenConnection())
            {
                conn.Execute(sql, box);
            }
        }

        var url = $"http://localhost:5000/api/boxes?searchTerm={searchterm}";
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url);
            TestContext.WriteLine("THE FULL BODY RESPONSE: " + await response.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            throw new Exception(Helper.NoResponseMessage, e);
        }

        var content = await response.Content.ReadAsStringAsync();
        IEnumerable<InStockBoxes> boxes;
        try
        {
            boxes = JsonConvert.DeserializeObject<IEnumerable<InStockBoxes>>(content) ??
                    throw new InvalidOperationException();
        }
        catch (Exception e)
        {
            throw new Exception(Helper.NoResponseMessage, e);
        }

        using (new AssertionScope())
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            boxes.Count().Should().Be(expectedResult);
        }
    }

    [Test]
    [TestCase("NonExistentResult")]
    public async Task BoxSearchNoResults(string searchterm)
    {

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(
                $"http://localhost:5000/api/boxes?searchTerm={searchterm}");
            TestContext.WriteLine("THE FULL BODY RESPONSE: " + await response.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            throw new Exception(Helper.NoResponseMessage, e);
        }

        var content = await response.Content.ReadAsStringAsync();
        IEnumerable<InStockBoxes> boxes;
        try
        {
            boxes = JsonConvert.DeserializeObject<IEnumerable<InStockBoxes>>(content) ??
                    throw new InvalidOperationException();
        }
        catch (Exception e)
        {
            throw new Exception(Helper.NoResponseMessage, e);
        }

        using (new AssertionScope())
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            boxes.Should().BeEmpty();
        }
    }
    
    
    // UI test where search is expected to succeed 
    [TestCase(1, "green")]
    [TestCase(2, "blue")]
    [TestCase(3, "clear")]
    public async Task CanSearchUI(int id, string color)
    {
        // ARRANGE
        Helper.TriggerRebuild();
        var box = new Box()
        {
            Id = 1,
            Size = "small",
            Weight = 1,
            Price = 1,
            Material = "plastic",
            Color = color,
            Quantity = 1
        };
        
        var sql = $@"
            insert into box_factory.boxes (size, weight, price, material, color, quantity) VALUES(@size, @weight,
                @price, @material, @color, @quantity)";
        using (var conn = Helper.DataSource.OpenConnection())
        {
            conn.Execute(sql, box);
        }

        
        
        //ACT
        
        Page.SetDefaultTimeout(3000);
        
        await Page.GotoAsync(Helper.ClientAppBaseUrl + "/boxes");
        
        await Page.GetByLabel("search text").ClickAsync();

        await Page.GetByLabel("search text").FillAsync(color);
        
        // ASSERT
        
        await Expect(Page.Locator("ion-card-header")).ToBeVisibleAsync();
    }
    
    
    
    // UI search test where search is expected to fail
    [TestCase(1, "plastic")]
    [TestCase(2, "metal")]
    [TestCase(3, "paper")]
    public async Task CanSearchUIFail(int id, string material)
    {
        // ARRANGE
        Helper.TriggerRebuild();
        var box = new Box()
        {
            Id = 1,
            Size = "small",
            Weight = 1,
            Price = 1,
            Material = material,
            Color = "green",
            Quantity = 1
        };
        
        var sql = $@"
            insert into box_factory.boxes (size, weight, price, material, color, quantity) VALUES(@size, @weight,
                @price, @material, @color, @quantity)";
        using (var conn = Helper.DataSource.OpenConnection())
        {
            conn.Execute(sql, box);
        }

        
        
        //ACT
        
        Page.SetDefaultTimeout(3000);
        
        await Page.GotoAsync(Helper.ClientAppBaseUrl + "/boxes");
        
        await Page.GetByLabel("search text").ClickAsync();

        await Page.GetByLabel("search text").FillAsync("wood");
        
        // ASSERT

        await Expect(Page.Locator("ion-card-header")).Not.ToBeVisibleAsync();
    }
}