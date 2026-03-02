using FluentValidation;

namespace JobBoard.Application.Actions.Resumes.Upload;

public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain"
    ];

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.OriginalFileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Only PDF, DOCX, and TXT files are allowed.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File cannot be empty.")
            .LessThanOrEqualTo(MaxFileSize).WithMessage("File size must not exceed 5 MB.");
    }
}
