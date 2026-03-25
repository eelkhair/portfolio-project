using System.Text;
using JobBoard.AI.Application.Actions.Resumes.Parse;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Utilities;

[Trait("Category", "Unit")]
public class ResumeTextExtractorTests
{
    [Fact]
    public void ExtractText_PlainText_ReturnsContent()
    {
        var text = "John Doe\nSenior Developer\nSkills: C#, .NET";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

        var result = ResumeTextExtractor.ExtractText(base64, "text/plain");

        result.ShouldBe(text);
    }

    [Fact]
    public void ExtractText_UnsupportedContentType_ThrowsNotSupported()
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("test"));

        Should.Throw<NotSupportedException>(
            () => ResumeTextExtractor.ExtractText(base64, "image/png"));
    }

    [Fact]
    public void ExtractText_EmptyPlainText_ReturnsEmpty()
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(""));

        var result = ResumeTextExtractor.ExtractText(base64, "text/plain");

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractEmail_ValidEmail_ExtractsIt()
    {
        var text = "Contact me at john.doe@example.com for more info";

        var result = ResumeTextExtractor.ExtractEmail(text);

        result.ShouldBe("john.doe@example.com");
    }

    [Fact]
    public void ExtractEmail_NoEmail_ReturnsEmpty()
    {
        var text = "No email address here";

        var result = ResumeTextExtractor.ExtractEmail(text);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractEmail_MultipleEmails_ReturnsFirst()
    {
        var text = "Primary: first@test.com, Secondary: second@test.com";

        var result = ResumeTextExtractor.ExtractEmail(text);

        result.ShouldBe("first@test.com");
    }

    [Fact]
    public void ExtractEmail_EmailWithPlus_ExtractsIt()
    {
        var text = "user+tag@example.com";

        var result = ResumeTextExtractor.ExtractEmail(text);

        result.ShouldBe("user+tag@example.com");
    }

    [Fact]
    public void ExtractPhone_USFormat_ExtractsIt()
    {
        var text = "Call me at 555-123-4567";

        var result = ResumeTextExtractor.ExtractPhone(text);

        result.ShouldBe("555-123-4567");
    }

    [Fact]
    public void ExtractPhone_WithParentheses_ExtractsIt()
    {
        var text = "Phone: (555) 987-6543";

        var result = ResumeTextExtractor.ExtractPhone(text);

        result.ShouldBe("(555) 987-6543");
    }

    [Fact]
    public void ExtractPhone_WithCountryCode_ExtractsIt()
    {
        var text = "International: +1-555-123-4567";

        var result = ResumeTextExtractor.ExtractPhone(text);

        result.ShouldBe("+1-555-123-4567");
    }

    [Fact]
    public void ExtractPhone_NoPhone_ReturnsEmpty()
    {
        var text = "No phone number here";

        var result = ResumeTextExtractor.ExtractPhone(text);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractPhone_WithDots_ExtractsIt()
    {
        var text = "Phone: 555.123.4567";

        var result = ResumeTextExtractor.ExtractPhone(text);

        result.ShouldBe("555.123.4567");
    }

    [Fact]
    public void ExtractPhone_WithSpaces_ExtractsIt()
    {
        var text = "Phone: 555 123 4567";

        var result = ResumeTextExtractor.ExtractPhone(text);

        result.ShouldBe("555 123 4567");
    }
}
