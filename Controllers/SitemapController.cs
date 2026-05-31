using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WaldauCastle.Options;

namespace WaldauCastle.Controllers;

public class SitemapController(IOptions<SiteSettings> siteSettings) : Controller
{
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Index()
    {
        var baseUrl = siteSettings.Value.BaseUrl.TrimEnd('/');
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

        var urls = new List<XElement>
        {
            CreateUrl(ns, baseUrl, "/", "daily", "1.0"),
            CreateUrl(ns, baseUrl, "/Event", "weekly", "0.9"),
            CreateUrl(ns, baseUrl, "/Excursion", "weekly", "0.9"),
            CreateUrl(ns, baseUrl, "/About", "monthly", "0.8"),
            CreateUrl(ns, baseUrl, "/Contacts", "monthly", "0.7"),
            CreateUrl(ns, baseUrl, "/privacy", "yearly", "0.5")
        };

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "urlset", urls));

        return Content(document.ToString(), "application/xml", Encoding.UTF8);
    }

    private static XElement CreateUrl(
        XNamespace ns,
        string baseUrl,
        string path,
        string changeFreq,
        string priority,
        DateTime? lastMod = null)
    {
        var element = new XElement(ns + "url",
            new XElement(ns + "loc", baseUrl + path),
            new XElement(ns + "changefreq", changeFreq),
            new XElement(ns + "priority", priority));

        if (lastMod.HasValue)
            element.Add(new XElement(ns + "lastmod", lastMod.Value.ToString("yyyy-MM-dd")));

        return element;
    }
}
