using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class TranslationTests
{
    [Theory]
    [InlineData("bien reçu", "fr", "en", "Roger that")]
    [InlineData("收到", "zh", "ja", "了解")]
    [InlineData("position halten", "de", "fr", "Tenez la position")]
    [InlineData("hostile contact", "en", "es", "Contacto hostil")]
    [InlineData("alvo abatido", "pt", "en", "Target down")]
    [InlineData("メーデー メーデー", "ja", "zh", "呼救 呼救")]
    public void TranslationService_TranslatePhrase_ShouldMapPhrasesCorrectly(string text, string fromLang, string toLang, string expected)
    {
        // WHEN
        string result = TranslationService.TranslatePhrase(text, fromLang, toLang);

        // THEN
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TranslationService_SameLanguage_ShouldNotTranslate()
    {
        // GIVEN
        string phrase = "Target down";

        // WHEN
        string result = TranslationService.TranslatePhrase(phrase, "en", "en");

        // THEN
        Assert.Equal(phrase, result);
    }

    [Fact]
    public void TranslationService_UnknownPhrase_ShouldReturnInput()
    {
        // GIVEN
        string phrase = "this is an unknown military command phrase";

        // WHEN
        string result = TranslationService.TranslatePhrase(phrase, "en", "fr");

        // THEN
        Assert.Equal(phrase, result);
    }
}
