using Microsoft.AspNetCore.Mvc;

namespace WaldauCastle.Helpers;

public static class StaticImageCss
{
    public static string BackgroundImageSet(IUrlHelper url, string pathWithoutExtension)
    {
        var webp = url.Content($"~/{pathWithoutExtension}.webp");
        var jpg = url.Content($"~/{pathWithoutExtension}.jpg");
        return $"background-image: url('{webp}'); background-image: image-set(url('{webp}') type('image/webp'), url('{jpg}') type('image/jpeg'));";
    }
}
