using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using EtimadScraper.Models;
using System.Net;
using System.Text;

namespace EtimadScraper.Services;

/// <summary>
/// Service for scraping detailed tender information by tender ID.
/// Uses HttpClient and HtmlAgilityPack for HTML parsing.
/// </summary>
public class TenderDetailsScraperService
{
    private readonly ILogger<TenderDetailsScraperService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TenderDetailsScraperService(
        ILogger<TenderDetailsScraperService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory.CreateClient("EtimadClient");
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ar-SA,ar;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    // ?????????????????????????????????????????????????????????????
    // Public entry point
    // ?????????????????????????????????????????????????????????????

    public async Task<TenderDetailsDto> ScrapeTenderDetailsAsync(string tenderId, bool includeRawHtml = false)
    {
        if (string.IsNullOrWhiteSpace(tenderId))
            throw new ArgumentException("Tender ID cannot be null or empty", nameof(tenderId));

        _logger.LogInformation("Scraping tender details for ID: {TenderId}", tenderId);

        try
        {
            var dto = await ScrapeTenderFromWebAsync(tenderId, includeRawHtml);

            if (dto.Metadata.IsSuccess)
            {
                _logger.LogInformation("Successfully scraped tender details for ID: {TenderId}", tenderId);
                return dto;
            }

            _logger.LogWarning("Scraping failed for tender {TenderId}, attempting cached data", tenderId);
            var cached = await GetCachedTenderDetailsAsync(tenderId);
            if (cached != null)
            {
                _logger.LogInformation("Returning cached data for tender {TenderId}", tenderId);
                return cached;
            }

            _logger.LogError("No data available for tender {TenderId}", tenderId);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing tender {TenderId}", tenderId);

            var cached = await GetCachedTenderDetailsAsync(tenderId);
            if (cached != null) return cached;

            return ErrorDto(tenderId, $"Failed to retrieve tender details: {ex.Message}");
        }
    }

    // ?????????????????????????????????????????????????????????????
    // Scraping
    // ?????????????????????????????????????????????????????????????

    private async Task<TenderDetailsDto> ScrapeTenderFromWebAsync(string tenderId, bool includeRawHtml)
    {
        _logger.LogInformation("Starting multi-endpoint scraping for tender {TenderId}", tenderId);

        var dto = new TenderDetailsDto
        {
            TenderId = tenderId,
            Metadata = new MetadataSection
            {
                IsSuccess = true,
                DataSource = "Scraped",
                ScrapedAt = DateTime.UtcNow
            },
            Debug = new DebugSection
            {
                AllFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            }
        };

        var htmlResponses = new Dictionary<string, string>();
        var errorMessages = new List<string>();

        try
        {
            var endpoints = new Dictionary<string, string>
            {
                { "Main",         $"https://tenders.etimad.sa/Tender/DetailsForVisitor?STenderId={Uri.EscapeDataString(tenderId)}" },
                { "Dates",        $"https://tenders.etimad.sa/Tender/GetTenderDatesViewComponenet?tenderIdStr={Uri.EscapeDataString(tenderId)}" },
                { "Relations",    $"https://tenders.etimad.sa/Tender/GetRelationsDetailsViewComponenet?tenderIdStr={Uri.EscapeDataString(tenderId)}" },
                { "Awarding",     $"https://tenders.etimad.sa/Tender/GetAwardingResultsForVisitorViewComponenet?tenderIdStr={Uri.EscapeDataString(tenderId)}" },
                { "LocalContent", $"https://tenders.etimad.sa/Tender/GetLocalContentDetailsViewComponenet?tenderIdStr={Uri.EscapeDataString(tenderId)}" }
            };

            var fetchTasks = endpoints.Select(async kvp =>
            {
                try
                {
                    var html = await FetchHtmlAsync(kvp.Value, kvp.Key);
                    return new { Key = kvp.Key, Html = html, Success = !string.IsNullOrEmpty(html) };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch {EndpointName} for tender {TenderId}", kvp.Key, tenderId);
                    return new { Key = kvp.Key, Html = string.Empty, Success = false };
                }
            });

            var results = await Task.WhenAll(fetchTasks);

            foreach (var result in results)
            {
                if (result.Success)
                {
                    htmlResponses[result.Key] = result.Html;
                    _logger.LogInformation("Fetched {EndpointName} for tender {TenderId}", result.Key, tenderId);
                }
                else
                {
                    errorMessages.Add($"Failed to fetch {result.Key} endpoint");
                }
            }

            if (!htmlResponses.ContainsKey("Main") || string.IsNullOrEmpty(htmlResponses["Main"]))
            {
                _logger.LogError("Main page fetch failed for tender {TenderId}", tenderId);
                return ErrorDto(tenderId, "Failed to fetch main tender page. " + string.Join("; ", errorMessages));
            }

            if (htmlResponses.TryGetValue("Main", out var mainHtml))
                ParseMainDetails(mainHtml, dto);

            if (htmlResponses.TryGetValue("Dates", out var datesHtml))
                ParseDatesDetails(datesHtml, dto);

            if (htmlResponses.TryGetValue("Relations", out var relationsHtml))
                ParseRelationsDetails(relationsHtml, dto);

            if (htmlResponses.TryGetValue("Awarding", out var awardingHtml))
                ParseAwardingResults(awardingHtml, dto);

            if (htmlResponses.TryGetValue("LocalContent", out var localContentHtml))
                ParseLocalContentDetails(localContentHtml, dto);

            // Log every stored key so mismatches are visible in output
            foreach (var kvp in dto.Debug.AllFields)
                _logger.LogInformation("AllField key=[{Key}] value=[{Value}]", kvp.Key,
                    kvp.Value.Length > 80 ? kvp.Value[..80] + "�" : kvp.Value);

            MapAllFieldsToDto(dto);

            if (includeRawHtml)
            {
                var sb = new StringBuilder();
                foreach (var kvp in htmlResponses)
                {
                    sb.AppendLine($"<!-- ===== {kvp.Key} Endpoint ===== -->");
                    sb.AppendLine(kvp.Value);
                    sb.AppendLine();
                }
                dto.Debug.RawHtml = sb.ToString();
            }

            _logger.LogInformation(
                "Completed scraping for {TenderId}. Endpoints: {Count}. AllFields: {FieldCount}",
                tenderId, htmlResponses.Count, dto.Debug.AllFields.Count);

            if (errorMessages.Count > 0)
                dto.Metadata.ErrorMessage = "Partial success: " + string.Join("; ", errorMessages);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-endpoint scraping for tender {TenderId}", tenderId);
            return ErrorDto(tenderId, $"Scraping error: {ex.Message}");
        }
    }

    // ?????????????????????????????????????????????????????????????
    // HTTP fetch
    // ?????????????????????????????????????????????????????????????

    private async Task<string> FetchHtmlAsync(string url, string endpointName)
    {
        _logger.LogDebug("Fetching {EndpointName} from: {Url}", endpointName, url);

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode is HttpStatusCode.Forbidden
                                    or HttpStatusCode.Unauthorized
                || (int)response.StatusCode == 429)
            {
                _logger.LogWarning("Request blocked for {EndpointName}. Status: {StatusCode}",
                    endpointName, response.StatusCode);
                return string.Empty;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch {EndpointName}. Status: {StatusCode}",
                    endpointName, response.StatusCode);
                return string.Empty;
            }

            var html = await response.Content.ReadAsStringAsync();

            if (html.Contains("captcha", StringComparison.OrdinalIgnoreCase) ||
                html.Contains("Access Denied", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Anti-bot protection detected for {EndpointName}", endpointName);
                return string.Empty;
            }

            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP request failed for {EndpointName}", endpointName);
            return string.Empty;
        }
    }

    // ?????????????????????????????????????????????????????????????
    // AllFields ? nested DTO sections
    // ?????????????????????????????????????????????????????????????

    private void MapAllFieldsToDto(TenderDetailsDto dto)
    {
        var f = dto.Debug.AllFields;
        if (f == null || f.Count == 0)
        {
            _logger.LogWarning("AllFields is empty. Skipping DTO mapping.");
            return;
        }

        dto.BasicInformation.Title                        = GetField(f, "اسم المنافسة");
        dto.BasicInformation.TenderNumberIAM              = GetField(f, "رقم المنافسة");
        dto.BasicInformation.ReferenceNumber              = GetField(f, "الرقم المرجعي");
        dto.BasicInformation.Purpose                      = GetField(f, "الغرض من المنافسة");
        dto.BasicInformation.DocumentsValue               = GetField(f, "قيمة وثائق المنافسة");
        dto.BasicInformation.Status                       = GetField(f, "حالة المنافسة");
        dto.BasicInformation.ContractDuration             = GetField(f, "مدة العقد");
        dto.BasicInformation.MaintenanceInsurance         = GetField(f, "هل التأمين من متطلبات المنافسة");
        dto.BasicInformation.CompetitionType              = GetField(f, "نوع المنافسة");
        dto.BasicInformation.Organization                 = GetField(f, "الجهة الحكومية", "الجهة الحكوميه");
        dto.BasicInformation.SubmissionMethod             = GetField(f, "طريقة تقديم العروض");
        dto.BasicInformation.InitialGuaranteeRequirements = GetField(f, "مطلوب ضمان الإبتدائي", "مطلوب ضمان ابتدائي");
        dto.BasicInformation.InitialGuaranteeTitle        = GetField(f, "عنوان الضمان الإبتدائى", "عنوان الضمان الابتدائي");
        dto.BasicInformation.InitialGuaranteeValue        = GetField(f, "قيمة الضمان الابتدائي", "قيمة الضمان الإبتدائي");
        dto.BasicInformation.FinalGuarantee               = GetField(f, "الضمان النهائي");
        dto.BasicInformation.RemainingTime                = GetField(f, "الوقت المتبقي", "المدة المتبقية");

        dto.DatesAndDeadlines.InquiryDeadline             = GetField(f, "آخر موعد لإستلام الإستفسارات", "آخر موعد لاستلام الاستفسارات");
        dto.DatesAndDeadlines.SubmissionDeadline          = GetField(f, "آخر موعد لتقديم العروض");
        dto.DatesAndDeadlines.OfferOpeningDate            = GetField(f, "تاريخ فتح العروض");
        dto.DatesAndDeadlines.TechnicalOfferOpeningDate   = GetField(f, "تاريخ فحص العروض");
        dto.DatesAndDeadlines.StopPeriod                  = GetField(f, "فترة التوقف");
        dto.DatesAndDeadlines.ExpectedAwardDate           = GetField(f, "التاريخ المتوقع للترسية");
        dto.DatesAndDeadlines.ActionStartDate             = GetField(f, "تاريخ بدء الأعمال / الخدمات");
        dto.DatesAndDeadlines.QuestionSubmissionStartDate = GetField(f, "بداية إرسال الأسئلة و الاستفسارات", "بداية إرسال الأسئلة و الإستفسارات");
        dto.DatesAndDeadlines.MaxQuestionResponseTime     = GetField(f, "اقصى مدة للاجابة على الاستفسارات", "أقصى مدة للإجابة على الإستفسارات");
        dto.DatesAndDeadlines.OpeningPlace                = GetField(f, "مكان فتح العرض");

        dto.ClassificationAndExecution.TenderCondition              = GetField(f, "مجال التصنيف");
        dto.ClassificationAndExecution.ExecutionLocation            = GetField(f, "مكان التنفيذ");
        dto.ClassificationAndExecution.Description                  = GetField(f, "التفاصيل");
        dto.ClassificationAndExecution.Category                     = GetField(f, "نشاط المنافسة");
        dto.ClassificationAndExecution.SupplyItemsIncluded          = GetField(f, "تشمل المنافسة على بنود توريد");
        dto.ClassificationAndExecution.ConstructionWorks            = GetField(f, "أعمال الإنشاء");
        dto.ClassificationAndExecution.MaintenanceAndOperationWorks = GetField(f, "أعمال الصيانة والتشغيل");

        dto.AwardingResults.AwardingResultStatus  = GetField(f, "Awarding_حالة الترسية", "Awarding_نتيجة الترسية");
        dto.AwardingResults.AwardingResultMessage = GetField(f, "Awarding_رسالة الترسية", "Awarding_ملاحظات الترسية");

        dto.LocalContent.LocalContentRequirements = GetField(f,
            "LocalContent_آليات المحتوى المحلي المطبقة في المنافسة",
            "LocalContentRequirements");

        _logger.LogInformation(
            "DTO mapping complete. Title={HasTitle} Org={HasOrg} Deadline={HasDeadline} Category={HasCat}",
            !string.IsNullOrEmpty(dto.BasicInformation.Title),
            !string.IsNullOrEmpty(dto.BasicInformation.Organization),
            !string.IsNullOrEmpty(dto.DatesAndDeadlines.SubmissionDeadline),
            !string.IsNullOrEmpty(dto.ClassificationAndExecution.Category));
    }

    /// <summary>Returns the first non-empty value for any of the given keys.</summary>
    private static string GetField(Dictionary<string, string> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return string.Empty;
    }

    // ?????????????????????????????????????????????????????????????
    // Per-endpoint parsers
    // ?????????????????????????????????????????????????????????????

    private void ParseMainDetails(string html, TenderDetailsDto dto)
    {
        try
        {
            var data = ParseLabelValueTable(html);
            foreach (var kvp in data)
                dto.Debug.AllFields[kvp.Key] = kvp.Value;
            _logger.LogInformation("Parsed {Count} fields from main page", data.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error parsing main details"); }
    }

    private void ParseDatesDetails(string html, TenderDetailsDto dto)
    {
        try
        {
            var data = ParseLabelValueTable(html);
            foreach (var kvp in data)
                dto.Debug.AllFields[kvp.Key] = kvp.Value;
            _logger.LogInformation("Parsed {Count} fields from dates tab", data.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error parsing dates details"); }
    }

    private void ParseRelationsDetails(string html, TenderDetailsDto dto)
    {
        try
        {
            var data = ParseLabelValueTable(html);
            foreach (var kvp in data)
                dto.Debug.AllFields[kvp.Key] = kvp.Value;
            _logger.LogInformation("Parsed {Count} fields from relations tab", data.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error parsing relations details"); }
    }

    private void ParseAwardingResults(string html, TenderDetailsDto dto)
    {
        try
        {
            var data = ParseLabelValueTable(html);
            foreach (var kvp in data)
                dto.Debug.AllFields["Awarding_" + kvp.Key] = kvp.Value;
            _logger.LogInformation("Parsed {Count} fields from awarding tab", data.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error parsing awarding results"); }
    }

    private void ParseLocalContentDetails(string html, TenderDetailsDto dto)
    {
        try
        {
            var data = ParseLabelValueTable(html);
            foreach (var kvp in data)
                dto.Debug.AllFields["LocalContent_" + kvp.Key] = kvp.Value;
            _logger.LogInformation("Parsed {Count} fields from local content tab", data.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error parsing local content details"); }
    }

    // ?????????????????????????????????????????????????????????????
    // HTML ? label/value extraction
    // ?????????????????????????????????????????????????????????????

    private Dictionary<string, string> ParseLabelValueTable(string html)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Strategy 1: table rows � direct td/th children only
            var tableRows = doc.DocumentNode.SelectNodes("//table//tr | //tbody//tr");
            if (tableRows != null)
            {
                foreach (var row in tableRows)
                {
                    var cells = row.SelectNodes("./td | ./th");
                    if (cells == null || cells.Count < 2) continue;

                    for (int i = 0; i + 1 < cells.Count; i += 2)
                    {
                        var label = CleanText(cells[i].InnerText);
                        var value = CleanText(cells[i + 1].InnerText);
                        if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                            result[label] = value;
                    }
                }
            }

            // Strategy 2: Bootstrap col pairs
            var rows = doc.DocumentNode.SelectNodes("//div[contains(@class,'row')]");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cols = row.SelectNodes(".//div[contains(@class,'col')]");
                    if (cols == null || cols.Count < 2) continue;

                    for (int i = 0; i + 1 < cols.Count; i += 2)
                    {
                        var label = CleanText(cols[i].InnerText);
                        var value = CleanText(cols[i + 1].InnerText);
                        if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                            result[label] = value;
                    }
                }
            }

            // Strategy 3: <label for="�">
            var labels = doc.DocumentNode.SelectNodes("//label");
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    var labelText = CleanText(label.InnerText);
                    if (string.IsNullOrWhiteSpace(labelText)) continue;

                    string? valueText = null;

                    var forAttr = label.GetAttributeValue("for", null);
                    if (!string.IsNullOrEmpty(forAttr))
                    {
                        var input = doc.DocumentNode.SelectSingleNode($"//*[@id='{forAttr}']");
                        if (input != null)
                            valueText = CleanText(input.GetAttributeValue("value", null) ?? input.InnerText);
                    }

                    if (string.IsNullOrEmpty(valueText))
                    {
                        var sibling = label.NextSibling;
                        while (sibling != null && sibling.NodeType != HtmlNodeType.Element)
                            sibling = sibling.NextSibling;
                        if (sibling != null)
                            valueText = CleanText(sibling.InnerText);
                    }

                    if (!string.IsNullOrWhiteSpace(valueText) && valueText != labelText)
                        result[labelText] = valueText;
                }
            }

            // Strategy 4: <dt> / <dd>
            var dts = doc.DocumentNode.SelectNodes("//dt");
            if (dts != null)
            {
                foreach (var dt in dts)
                {
                    var labelText = CleanText(dt.InnerText);
                    var dd = dt.SelectSingleNode("following-sibling::dd[1]");
                    if (dd != null && !string.IsNullOrWhiteSpace(labelText))
                    {
                        var valueText = CleanText(dd.InnerText);
                        if (!string.IsNullOrWhiteSpace(valueText))
                            result[labelText] = valueText;
                    }
                }
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in ParseLabelValueTable"); }

        return result;
    }

    // ?????????????????????????????????????????????????????????????
    // Helpers
    // ?????????????????????????????????????????????????????????????

    private static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static TenderDetailsDto ErrorDto(string tenderId, string message) => new()
    {
        TenderId = tenderId,
        Metadata = new MetadataSection
        {
            IsSuccess = false,
            ErrorMessage = message,
            DataSource = "Error",
            ScrapedAt = DateTime.UtcNow
        }
    };

    // ?????????????????????????????????????????????????????????????
    // Cache stubs � replace with real DB logic when ready
    // ?????????????????????????????????????????????????????????????

    private async Task<TenderDetailsDto?> GetCachedTenderDetailsAsync(string tenderId)
    {
        _logger.LogDebug("Cache retrieval not implemented yet for tender {TenderId}", tenderId);
        await Task.CompletedTask;
        return null;
    }

    private async Task SaveToCacheAsync(TenderDetailsDto dto)
    {
        _logger.LogDebug("Cache save not implemented yet for tender {TenderId}", dto.TenderId);
        await Task.CompletedTask;
    }
}
