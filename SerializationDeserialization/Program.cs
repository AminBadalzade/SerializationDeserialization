// File: Program.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

public class Purchase
{
    public string ProductName { get; set; }
    public DateTime DateTime { get; set; }
    public decimal ProductPrice { get; set; }
}

public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
}

public class Admin : User
{
    public string AdminLevel { get; set; }
}

public class RegularUser : User
{
    public int LoyaltyPoints { get; set; }
}

// Used to preserve runtime type info
public class UserConverter : JsonConverter<User>
{
    public override bool CanConvert(Type typeToConvert) => typeof(User).IsAssignableFrom(typeToConvert);

    public override User Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var type = root.GetProperty("TypeDiscriminator").GetString();
        return type switch
        {
            "Admin" => JsonSerializer.Deserialize<Admin>(root.GetRawText(), options),
            "RegularUser" => JsonSerializer.Deserialize<RegularUser>(root.GetRawText(), options),
            _ => throw new JsonException("Unknown type discriminator"),
        };
    }

    public override void Write(Utf8JsonWriter writer, User value, JsonSerializerOptions options)
    {
        var type = value switch
        {
            Admin => "Admin",
            RegularUser => "RegularUser",
            _ => throw new JsonException("Unknown type")
        };

        var json = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        var jsonObject = new Dictionary<string, JsonElement>
        {
            { "TypeDiscriminator", JsonSerializer.SerializeToElement(type) }
        };

        foreach (var prop in json.EnumerateObject())
            jsonObject[prop.Name] = prop.Value;

        JsonSerializer.Serialize(writer, jsonObject, options);
    }
}

class Program
{
    static void Main(string[] args)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new UserConverter());

        // 1. Create sample purchases.json manually (or simulate here)
        var purchases = new List<Purchase>
        {
            new Purchase { ProductName = "Orange", DateTime = DateTime.UtcNow, ProductPrice = 2.49m },
            new Purchase { ProductName = "Apple", DateTime = DateTime.UtcNow, ProductPrice = 1.99m }
        };

        File.WriteAllText("purchases.json", JsonSerializer.Serialize(purchases, options));

        // 2. Deserialize and loop output
        var purchasesFromFile = JsonSerializer.Deserialize<List<Purchase>>(File.ReadAllText("purchases.json"));
        Console.WriteLine("\n--- Purchases:");
        foreach (var p in purchasesFromFile)
        {
            Console.WriteLine($"{p.ProductName}, {p.ProductPrice:C}, {p.DateTime}");
        }

        // 3. XML reader simulation
        string xmlData = "<Purchase><ProductName>Banana</ProductName><DateTime>2023-10-01T12:00:00</DateTime><ProductPrice>0.99</ProductPrice></Purchase>";
        var serializer = new XmlSerializer(typeof(Purchase));
        using var reader = new StringReader(xmlData);
        var xmlPurchase = (Purchase)serializer.Deserialize(reader);
        Console.WriteLine("\n--- XML Parsed Purchase:");
        Console.WriteLine($"{xmlPurchase.ProductName}, {xmlPurchase.ProductPrice:C}, {xmlPurchase.DateTime}");

        // 4. Create and serialize User types
        var users = new List<User>
        {
            new Admin { Username = "admin_john", Email = "john@admin.com", AdminLevel = "Super" },
            new RegularUser { Username = "user_anna", Email = "anna@user.com", LoyaltyPoints = 150 }
        };

        File.WriteAllText("users.json", JsonSerializer.Serialize(users, options));

        // 5. Deserialize and output user types
        var usersFromFile = JsonSerializer.Deserialize<List<User>>(File.ReadAllText("users.json"), options);
        Console.WriteLine("\n--- Users:");
        foreach (var user in usersFromFile)
        {
            switch (user)
            {
                case Admin a:
                    Console.WriteLine($"[Admin] {a.Username} - {a.Email} - Level: {a.AdminLevel}");
                    break;
                case RegularUser r:
                    Console.WriteLine($"[User] {r.Username} - {r.Email} - Points: {r.LoyaltyPoints}");
                    break;
            }
        }
    }
}