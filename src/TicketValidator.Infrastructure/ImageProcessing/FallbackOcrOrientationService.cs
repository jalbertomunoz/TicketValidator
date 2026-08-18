using Microsoft.Extensions.Logging;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Application.Services;

namespace TicketValidator.Infrastructure.ImageProcessing;

public sealed class FallbackOcrOrientationService : IOcrOrientationService
{
    private const int MinimumUsefulWordCount = 3;
    private const int MaximumScoredWordCount = 50;
    private const decimal WordScore = 100m;
    private const decimal ConfidenceScore = 100m;
    private const decimal DateBonus = 10_000m;
    private const decimal TotalBonus = 10_000m;
    private static readonly int[] FallbackRotations = [90, 180, 270];

    private readonly IDocumentOrientationService _documentOrientationService;
    private readonly IOcrService _ocrService;
    private readonly ILogger<FallbackOcrOrientationService> _logger;
    private readonly Func<byte[], int, byte[]> _rotateClockwise;

    public FallbackOcrOrientationService(
        IDocumentOrientationService documentOrientationService,
        IOcrService ocrService,
        ILogger<FallbackOcrOrientationService> logger)
        : this(documentOrientationService, ocrService, logger, OrthogonalImageRotation.RotateClockwise)
    {
    }

    internal FallbackOcrOrientationService(
        IDocumentOrientationService documentOrientationService,
        IOcrService ocrService,
        ILogger<FallbackOcrOrientationService> logger,
        Func<byte[], int, byte[]> rotateClockwise)
    {
        _documentOrientationService = documentOrientationService ?? throw new ArgumentNullException(nameof(documentOrientationService));
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rotateClockwise = rotateClockwise ?? throw new ArgumentNullException(nameof(rotateClockwise));
    }

    public async Task<OcrOrientationResult> ReadBestAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Length == 0)
        {
            throw new ArgumentException("The image cannot be empty.", nameof(image));
        }

        var osdImage = await _documentOrientationService.OrientAsync(image, cancellationToken);
        var initialOcr = await _ocrService.ReadAsync(osdImage, cancellationToken);
        var initialCandidate = CreateCandidate(0, osdImage, initialOcr, isInitial: true);
        if (!IsInsufficient(initialCandidate.OcrResult, initialCandidate.Evidence))
        {
            return ToResult(initialCandidate);
        }

        var candidates = new List<Candidate> { initialCandidate };
        var bestCandidate = initialCandidate;
        foreach (var rotation in FallbackRotations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rotatedImage = _rotateClockwise(osdImage, rotation);
            var ocrResult = await _ocrService.ReadAsync(rotatedImage, cancellationToken);
            var candidate = CreateCandidate(rotation, rotatedImage, ocrResult, isInitial: false);
            candidates.Add(candidate);
            if (IsBetter(candidate, bestCandidate))
            {
                bestCandidate = candidate;
            }
        }

        _logger.LogInformation(
            "OCR orientation fallback selected rotation {SelectedRotation}. Candidates: {Candidates}",
            bestCandidate.Rotation,
            string.Join(", ", candidates.Select(candidate =>
                $"{candidate.Rotation}:words={candidate.Evidence.WordCount},score={candidate.Score}")));

        return ToResult(bestCandidate);
    }

    private static bool IsInsufficient(OcrResult ocrResult, OcrEvidenceAnalysis evidence) =>
        string.IsNullOrWhiteSpace(ocrResult.RawText) || evidence.WordCount < MinimumUsefulWordCount;

    private static Candidate CreateCandidate(int rotation, byte[] image, OcrResult ocrResult, bool isInitial)
    {
        var evidence = OcrEvidenceAnalyzer.Analyze(ocrResult);
        var score = Math.Min(evidence.WordCount, MaximumScoredWordCount) * WordScore
            + Math.Clamp(ocrResult.MeanConfidence ?? 0m, 0m, 1m) * ConfidenceScore
            + (evidence.Date is null ? 0m : DateBonus)
            + (evidence.Total is null ? 0m : TotalBonus);

        return new Candidate(rotation, image, ocrResult, evidence, score, isInitial);
    }

    private static bool IsBetter(Candidate candidate, Candidate current) =>
        candidate.Score > current.Score
        || candidate.Score == current.Score && candidate.IsInitial && !current.IsInitial
        || candidate.Score == current.Score && candidate.IsInitial == current.IsInitial && candidate.Rotation < current.Rotation;

    private static OcrOrientationResult ToResult(Candidate candidate) => new()
    {
        Image = candidate.Image,
        OcrResult = candidate.OcrResult,
        SelectedRotation = candidate.Rotation
    };

    private sealed record Candidate(
        int Rotation,
        byte[] Image,
        OcrResult OcrResult,
        OcrEvidenceAnalysis Evidence,
        decimal Score,
        bool IsInitial);
}
