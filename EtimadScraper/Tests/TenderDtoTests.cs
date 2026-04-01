using Xunit;
using EtimadScraper.Models;
using System.Text.Json;

namespace EtimadScraper.Tests;

/// <summary>
/// Unit tests for TenderDto model
/// Run with: dotnet test
/// </summary>
public class TenderDtoTests
{
    [Fact]
    public void TenderDto_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var tender = new TenderDto();

        // Assert
        Assert.NotNull(tender);
        Assert.Equal(string.Empty, tender.TenderNumber);
        Assert.Equal(string.Empty, tender.Title);
        Assert.Equal(string.Empty, tender.Organization);
        Assert.NotEqual(DateTime.MinValue, tender.ScrapedAt);
    }

    [Fact]
    public void TenderDto_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var tender = new TenderDto
        {
            TenderNumber = "12345",
            Title = "Test Tender",
            Organization = "Test Org"
        };

        // Act
        var result = tender.ToString();

        // Assert
        Assert.Contains("12345", result);
        Assert.Contains("Test Tender", result);
        Assert.Contains("Test Org", result);
    }

    [Fact]
    public void TenderDto_JsonSerialization_ShouldSerializeCorrectly()
    {
        // Arrange
        var tender = new TenderDto
        {
            TenderNumber = "12345",
            Title = "Test Tender",
            Organization = "Test Org",
            PublishDate = "01/01/2024",
            ClosingDate = "15/01/2024",
            DetailsUrl = "https://example.com/tender/12345",
            Status = "Active"
        };

        // Act
        var json = JsonSerializer.Serialize(tender);
        var deserialized = JsonSerializer.Deserialize<TenderDto>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(tender.TenderNumber, deserialized.TenderNumber);
        Assert.Equal(tender.Title, deserialized.Title);
        Assert.Equal(tender.Organization, deserialized.Organization);
    }

    [Fact]
    public void TenderDto_Properties_ShouldBeSettable()
    {
        // Arrange
        var tender = new TenderDto();

        // Act
        tender.TenderNumber = "67890";
        tender.Title = "New Tender";
        tender.Organization = "New Org";
        tender.PublishDate = "20/01/2024";
        tender.ClosingDate = "30/01/2024";
        tender.DetailsUrl = "https://example.com/tender/67890";
        tender.Status = "Closed";
        tender.AdditionalInfo = "Test Info";

        // Assert
        Assert.Equal("67890", tender.TenderNumber);
        Assert.Equal("New Tender", tender.Title);
        Assert.Equal("New Org", tender.Organization);
        Assert.Equal("20/01/2024", tender.PublishDate);
        Assert.Equal("30/01/2024", tender.ClosingDate);
        Assert.Equal("https://example.com/tender/67890", tender.DetailsUrl);
        Assert.Equal("Closed", tender.Status);
        Assert.Equal("Test Info", tender.AdditionalInfo);
    }
}

/// <summary>
/// Unit tests for ScraperConfiguration
/// </summary>
public class ScraperConfigurationTests
{
    [Fact]
    public void ScraperConfiguration_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new EtimadScraper.Configuration.ScraperConfiguration();

        // Assert
        Assert.NotNull(config.BaseUrl);
        Assert.True(config.Headless);
        Assert.True(config.MaxPages > 0);
        Assert.True(config.StartPage > 0);
        Assert.True(config.PageLoadTimeout > 0);
        Assert.NotNull(config.OutputFilePath);
    }

    [Fact]
    public void ScraperConfiguration_CustomValues_ShouldBeSettable()
    {
        // Arrange
        var config = new EtimadScraper.Configuration.ScraperConfiguration();

        // Act
        config.MaxPages = 10;
        config.Headless = false;
        config.DelayBetweenPages = 5000;

        // Assert
        Assert.Equal(10, config.MaxPages);
        Assert.False(config.Headless);
        Assert.Equal(5000, config.DelayBetweenPages);
    }
}

// Note: To use these tests, create a test project:
// dotnet new xunit -n EtimadScraper.Tests
// dotnet add reference ../EtimadScraper/EtimadScraper.csproj
// Then copy this file to the test project
