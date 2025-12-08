using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SmartReader;
using ReverseMarkdown;
using AngleSharp;
using AngleSharp.Html.Parser;

namespace Alkampfer.Assistant.Core.Bookmarks;

public class ContentExtractionService : IContentExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly IFileStore _fileStore;
    private readonly ILogger<ContentExtractionService> _logger;

    public ContentExtractionService(HttpClient httpClient, IFileStore fileStore, ILogger<ContentExtractionService> logger)
    {
        _httpClient = httpClient;
        _fileStore = fileStore;
        _logger = logger;
    }

    public async Task<ExtractionResult> ExtractAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting extraction for {Url}", url);

        string html;
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            html = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Successfully fetched HTML for {Url}. Length: {Length}", url, html.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch URL {Url}", url);
            throw;
        }

        var reader = new Reader(url, html);
        var article = await reader.GetArticleAsync();

        if (IsExtractionInsufficient(article))
        {
            _logger.LogInformation("SmartReader extraction insufficient for {Url} (Length: {Length}), trying Playwright", url, article.Content?.Length ?? 0);
            try 
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                var page = await browser.NewPageAsync();
                await page.GotoAsync(url);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                html = await page.ContentAsync();
                
                reader = new Reader(url, html);
                article = await reader.GetArticleAsync();
                _logger.LogInformation("Playwright extraction completed for {Url}. Content Length: {Length}", url, article.Content?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Playwright fallback failed for {Url}", url);
            }
        }
        else
        {
            _logger.LogDebug("SmartReader extraction successful for {Url}. Content Length: {Length}", url, article.Content?.Length ?? 0);
        }

        var config = new ReverseMarkdown.Config
        {
            UnknownTags = Config.UnknownTagsOption.Bypass,
            GithubFlavored = true,
            RemoveComments = true
        };
        var converter = new ReverseMarkdown.Converter(config);
        
        var contentHtml = article.Content;
        var attachments = new List<Attachment>();
        
        try 
        {
            var parser = new AngleSharp.Html.Parser.HtmlParser();
            var document = await parser.ParseDocumentAsync(contentHtml);
            var images = document.QuerySelectorAll("img");
            _logger.LogDebug("Found {Count} images in content for {Url}", images.Length, url);
            
            foreach (var img in images)
            {
                var src = img.GetAttribute("src");
                if (string.IsNullOrEmpty(src)) continue;
                
                if (!Uri.TryCreate(new Uri(url), src, out var imgUri)) continue;
                
                try 
                {
                    var imgBytes = await _httpClient.GetByteArrayAsync(imgUri, cancellationToken);
                    var extension = Path.GetExtension(imgUri.AbsolutePath);
                    if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                    
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = $"images/{fileName}";
                    
                    using var stream = new MemoryStream(imgBytes);
                    await _fileStore.SaveFileAsync(filePath, stream, cancellationToken);
                    
                    attachments.Add(new Attachment(fileName, filePath));
                    
                    // Update src to point to the stored path
                    // Note: The UI will need to handle serving this, or we generate a public URL here if possible.
                    // For now, we store the relative path in the IFileStore.
                    img.SetAttribute("src", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download image {ImgUrl}", imgUri);
                }
            }
            contentHtml = document.Body?.InnerHtml ?? contentHtml;
            _logger.LogInformation("Processed {Count} images for {Url}", attachments.Count, url);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error processing images for {Url}", url);
        }
        
        var markdown = converter.Convert(contentHtml);
        _logger.LogInformation("Extraction completed for {Url}. Markdown Length: {Length}", url, markdown.Length);
        
        return new ExtractionResult(markdown, article.Title ?? "No Title", article.Excerpt ?? "", attachments);
    }

    private bool IsExtractionInsufficient(Article article)
    {
        return string.IsNullOrWhiteSpace(article.Content) || article.Content.Length < 200;
    }
}
