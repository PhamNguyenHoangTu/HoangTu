using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MangaReader.DomainCommon;

namespace MangaReader.MangaList;

public class Manga
{
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string CoverUrl { get; init; } = null!;
    //hdcphu@ updated for https://apptruyen247.com
    public string LastChapter { get; init; } = null!;
    public string MangaUrl { get; init; } = null!;
}

public class MangaList
{
    public int TotalMangaNumber { get; init; }
    public int TotalPageNumber { get; init; }
    public List<Manga> CurrentPage { get; init; } = null!;
}

public class Domain
{
    private readonly string baseUrl;
    private readonly Http http;

    public Domain(string baseUrl, Http http)
    {
        this.baseUrl = baseUrl;
        this.http = http;
    }

    private async Task<string> DownloadHtml(int page)
    {
        if (page < 1) page = 1;
        //hdcphu@ updated for https://apptruyen247.com
        var url = $"{this.baseUrl}/filter?status=0&sort=updatedAt&page={page}";
        Console.WriteLine($"Downloading page {page} from {url}");
        return await http.GetStringAsync(url);
    }

    private int ParseTotalMangaNumber(XmlDocument doc)
    {
        //hdcphu@ updated for https://apptruyen247.com
        var text = doc.DocumentElement!.FirstChild!.FirstChild!.InnerText.Trim();
        var number = text.Substring(7);
        return int.Parse(number);
    }

    //hdcphu@ updated for https://apptruyen247.com
    // private int ParseTotalPageNumber(XmlDocument doc)
    // {
    //     var div = doc.DocumentElement!.ChildNodes[3]!;
    //     var span = div.LastChild!;
    //     if (span.Attributes!["class"]!.Value == "current_page")
    //         return int.Parse(span.InnerText);
    //     var href = span.FirstChild!.Attributes!["href"]!.Value;
    //     var openingParenthesisIndex = href.LastIndexOf('(');
    //     var number = href.Substring(openingParenthesisIndex + 1, href.Length - openingParenthesisIndex - 2);
    //     return int.Parse(number);
    // }

    private List<Manga> ParseMangaList(XmlDocument doc)
    {
        //hdcphu@ updated for https://apptruyen247.com
        var div = doc.DocumentElement!.FirstChild!.ChildNodes![1];
        var nodes = div.ChildNodes;
        var mangaList = new List<Manga>();
        for (int i = 0; i < nodes.Count; i++)
        {
            var nodeF1 = nodes[i]!.FirstChild!;
            var nodeUrlInfo = nodeF1.FirstChild!;
            var nodeTitleInfo = nodeF1.ChildNodes[1]!;
            
            var title = Html.Decode(nodeTitleInfo.FirstChild!.InnerText.Trim());
            var description = Html.Decode(nodeTitleInfo.ChildNodes[1]!.InnerText.Trim());
            var lastChapter = nodeTitleInfo.ChildNodes[2]!.FirstChild != null ? Html.Decode(nodeTitleInfo.ChildNodes[2]!.FirstChild?.InnerText!.Trim()!): "";
            
            var coverUrl = baseUrl + nodeUrlInfo.FirstChild!.Attributes!["src"]!.Value;
            var mangaUrl = baseUrl + nodeUrlInfo.Attributes!["href"]!.Value;
            
            var manga = new Manga
            { 
                Title = title,
                Description = description,
                CoverUrl = coverUrl,
                LastChapter = lastChapter,
                MangaUrl = mangaUrl
            };
            mangaList.Add(manga);
        }
        return mangaList;
    }

    private MangaList Parse(string html)
    {
        try
        {
            var doc = new XmlDocument();            
            //hdcphu@ updated for https://apptruyen247.com
            var xmlStartAt = html.IndexOf("<div hidden id=");
            if (xmlStartAt > 0) {
                Console.WriteLine("Page is hidden, Retry later!!!");
            }
            else
            {
                xmlStartAt = html.IndexOf("<!--/$--><!--$-->");
                var offset = 17;
                if (xmlStartAt < 0)
                {
                    xmlStartAt = html.IndexOf("<!--/$--><!--$?-->");
                    offset = 18;
                }

                if (xmlStartAt > 0)
                {
                    Console.WriteLine("Load normal page");
                    html = html.Substring(xmlStartAt + offset);
                    html = html.Substring(0,html.IndexOf("<!--/$-->"));
                }
            }
            try
            {
                doc.LoadXml("<root>" + html + "</root>");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            Console.WriteLine("Page loaded");
            var totalMangaNumber = ParseTotalMangaNumber(doc);
            var totalPageNumber = totalMangaNumber / 18;
            totalPageNumber += totalMangaNumber % 18 > 0 ? 1 : 0;
            
            Console.WriteLine($"Got {totalMangaNumber} manga(s) of {totalPageNumber} pages");
            var page = ParseMangaList(doc);
            
            return new MangaList
            {
                TotalMangaNumber = totalMangaNumber,
                TotalPageNumber = totalPageNumber,
                CurrentPage = page
            };
        }
        catch (Exception e)
        {
            throw new ParseException();
        }
    }

    public async Task<MangaList> LoadMangaList(int page)
    {
        var html = await DownloadHtml(page);
        return this.Parse(html);
    }

    public Task<byte[]> LoadBytes(string url, CancellationToken token)
    {
        return http.GetBytesAsync(url, token);
    }
}