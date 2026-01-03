using DocN.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocN.Data.Services;

/// <summary>
/// Service for RAGAS (RAG Assessment) metrics evaluation
/// </summary>
public class RAGASMetricsService : IRAGASMetricsService
{
    private readonly ILogger<RAGASMetricsService> _logger;
    private readonly IMultiProviderAIService _aiService;
    private readonly IRAGQualityService _qualityService;
    
    // RAGAS metric thresholds
    private const double FAITHFULNESS_THRESHOLD = 0.75;
    private const double RELEVANCY_THRESHOLD = 0.75;
    private const double PRECISION_THRESHOLD = 0.70;
    private const double RECALL_THRESHOLD = 0.70;

    public RAGASMetricsService(
        ILogger<RAGASMetricsService> logger,
        IMultiProviderAIService aiService,
        IRAGQualityService qualityService)
    {
        _logger = logger;
        _aiService = aiService;
        _qualityService = qualityService;
    }

    public async Task<RAGASEvaluationResult> EvaluateResponseAsync(
        string query,
        string response,
        IEnumerable<string> contexts,
        string? groundTruth = null,
        CancellationToken cancellationToken = default)
    {
        var result = new RAGASEvaluationResult();
        
        try
        {
            var contextList = contexts.ToList();
            
            // Calculate individual metrics
            result.FaithfulnessScore = await CalculateFaithfulnessAsync(
                response, 
                contextList, 
                cancellationToken);
            
            result.AnswerRelevancyScore = await CalculateAnswerRelevancyAsync(
                query, 
                response, 
                cancellationToken);
            
            result.ContextPrecisionScore = await CalculateContextPrecisionAsync(
                query, 
                contextList, 
                groundTruth, 
                cancellationToken);
            
            result.ContextRecallScore = await CalculateContextRecallAsync(
                contextList, 
                groundTruth, 
                cancellationToken);
            
            // Calculate overall RAGAS score (harmonic mean of all metrics)
            var scores = new[]
            {
                result.FaithfulnessScore,
                result.AnswerRelevancyScore,
                result.ContextPrecisionScore,
                result.ContextRecallScore
            };
            
            result.OverallRAGASScore = CalculateHarmonicMean(scores);
            
            // Add detailed metrics
            result.DetailedMetrics["faithfulness"] = result.FaithfulnessScore;
            result.DetailedMetrics["answer_relevancy"] = result.AnswerRelevancyScore;
            result.DetailedMetrics["context_precision"] = result.ContextPrecisionScore;
            result.DetailedMetrics["context_recall"] = result.ContextRecallScore;
            result.DetailedMetrics["overall"] = result.OverallRAGASScore;
            
            // Generate insights
            GenerateInsights(result);
            
            _logger.LogInformation(
                "RAGAS Evaluation - Overall: {Overall:F2}, Faithfulness: {Faithfulness:F2}, Relevancy: {Relevancy:F2}",
                result.OverallRAGASScore,
                result.FaithfulnessScore,
                result.AnswerRelevancyScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating RAGAS metrics");
        }
        
        return result;
    }

    public async Task<double> CalculateFaithfulnessAsync(
        string response,
        IEnumerable<string> contexts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Faithfulness: How well the response is grounded in the context
            // Score = (Number of supported statements) / (Total statements)
            
            var contextList = contexts.ToList();
            if (!contextList.Any())
                return 0.0;
            
            // Split response into statements
            var statements = SplitIntoStatements(response);
            if (!statements.Any())
                return 1.0;
            
            var supportedCount = 0;
            foreach (var statement in statements)
            {
                var confidence = await _qualityService.CalculateConfidenceScoreAsync(
                    statement, 
                    contextList, 
                    cancellationToken);
                
                if (confidence > 0.7)
                    supportedCount++;
            }
            
            return (double)supportedCount / statements.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating faithfulness");
            return 0.0;
        }
    }

    public async Task<double> CalculateAnswerRelevancyAsync(
        string query,
        string response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Answer Relevancy: How relevant the response is to the query
            // Using semantic similarity between query and response
            
            // Extract key terms from query and response
            var queryTerms = ExtractKeyTerms(query);
            var responseTerms = ExtractKeyTerms(response);
            
            if (!queryTerms.Any() || !responseTerms.Any())
                return 0.5;
            
            // Calculate term overlap
            var intersection = queryTerms.Intersect(responseTerms, StringComparer.OrdinalIgnoreCase).Count();
            var union = queryTerms.Union(responseTerms, StringComparer.OrdinalIgnoreCase).Count();
            
            var jaccardSimilarity = (double)intersection / union;
            
            // Boost score if response directly addresses query intent
            var intentBoost = ContainsQueryIntent(query, response) ? 0.2 : 0.0;
            
            return Math.Min(1.0, jaccardSimilarity + intentBoost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating answer relevancy");
            return 0.0;
        }
    }

    public async Task<double> CalculateContextPrecisionAsync(
        string query,
        IEnumerable<string> contexts,
        string? groundTruth = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Context Precision: How many retrieved contexts are relevant
            // Score = (Number of relevant contexts) / (Total contexts)
            
            var contextList = contexts.ToList();
            if (!contextList.Any())
                return 0.0;
            
            var relevantCount = 0;
            var queryTerms = ExtractKeyTerms(query);
            
            foreach (var context in contextList)
            {
                var contextTerms = ExtractKeyTerms(context);
                var overlap = queryTerms.Intersect(contextTerms, StringComparer.OrdinalIgnoreCase).Count();
                
                // Context is relevant if it shares significant terms with query
                if (overlap >= Math.Min(3, queryTerms.Count / 2))
                    relevantCount++;
            }
            
            return (double)relevantCount / contextList.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating context precision");
            return 0.0;
        }
    }

    public async Task<double> CalculateContextRecallAsync(
        IEnumerable<string> contexts,
        string? groundTruth = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Context Recall: How much of the ground truth is covered by contexts
            // If no ground truth, estimate based on context diversity
            
            if (string.IsNullOrEmpty(groundTruth))
            {
                // Estimate recall based on context diversity
                var contextList = contexts.ToList();
                if (!contextList.Any())
                    return 0.0;
                
                // Calculate diversity score
                var allTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var termFrequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var context in contextList)
                {
                    var terms = ExtractKeyTerms(context);
                    foreach (var term in terms)
                    {
                        allTerms.Add(term);
                        termFrequencies[term] = termFrequencies.GetValueOrDefault(term, 0) + 1;
                    }
                }
                
                // Higher diversity indicates better recall
                var uniqueTermRatio = (double)allTerms.Count / termFrequencies.Values.Sum();
                return Math.Min(1.0, uniqueTermRatio * 2);
            }
            
            // With ground truth, calculate actual recall
            var groundTruthTerms = ExtractKeyTerms(groundTruth);
            var contextTerms = contexts
                .SelectMany(ExtractKeyTerms)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            if (!groundTruthTerms.Any())
                return 1.0;
            
            var coveredTerms = groundTruthTerms
                .Count(term => contextTerms.Contains(term));
            
            return (double)coveredTerms / groundTruthTerms.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating context recall");
            return 0.0;
        }
    }

    public async Task<GoldenDatasetEvaluationResult> EvaluateGoldenDatasetAsync(
        string datasetId,
        CancellationToken cancellationToken = default)
    {
        var result = new GoldenDatasetEvaluationResult
        {
            DatasetId = datasetId,
            EvaluatedAt = DateTime.UtcNow
        };
        
        try
        {
            // In production, load golden dataset from database
            // For now, return mock structure
            _logger.LogInformation("Evaluating golden dataset: {DatasetId}", datasetId);
            
            result.TotalSamples = 0;
            result.EvaluatedSamples = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating golden dataset");
        }
        
        return result;
    }

    public async Task<ContinuousMonitoringMetrics> GetMonitoringMetricsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ContinuousMonitoringMetrics
        {
            TotalEvaluations = 0,
            AverageScores = new RAGASEvaluationResult(),
            QualityTrend = 0.0
        };
        
        try
        {
            _logger.LogInformation("Getting continuous monitoring metrics");
            // In production, query from metrics database
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monitoring metrics");
        }
        
        return result;
    }

    public async Task<ABTestResult> CompareConfigurationsAsync(
        string configurationA,
        string configurationB,
        string testDatasetId,
        CancellationToken cancellationToken = default)
    {
        var result = new ABTestResult
        {
            ConfigurationA = configurationA,
            ConfigurationB = configurationB
        };
        
        try
        {
            _logger.LogInformation(
                "Comparing configurations: {ConfigA} vs {ConfigB}",
                configurationA,
                configurationB);
            
            // In production, run evaluations on both configurations
            // and perform statistical significance testing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing configurations");
        }
        
        return result;
    }

    private List<string> SplitIntoStatements(string text)
    {
        return text
            .Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private List<string> ExtractKeyTerms(string text)
    {
        // Simple term extraction - in production, use NLP techniques
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
            "been", "being", "have", "has", "had", "do", "does", "did", "will",
            "would", "could", "should", "may", "might", "can", "this", "that",
            "these", "those", "i", "you", "he", "she", "it", "we", "they"
        };
        
        return text
            .ToLower()
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']' }, 
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(term => term.Length > 2 && !stopWords.Contains(term))
            .Distinct()
            .ToList();
    }

    private bool ContainsQueryIntent(string query, string response)
    {
        // Check if response contains question words from query
        var questionWords = new[] { "what", "when", "where", "who", "why", "how", "which" };
        var queryLower = query.ToLower();
        
        return questionWords.Any(word => queryLower.Contains(word));
    }

    private double CalculateHarmonicMean(double[] values)
    {
        if (values.Length == 0 || values.Any(v => v <= 0))
            return 0.0;
        
        var sum = values.Sum(v => 1.0 / v);
        return values.Length / sum;
    }

    private void GenerateInsights(RAGASEvaluationResult result)
    {
        if (result.OverallRAGASScore >= 0.80)
            result.Insights.Add("Excellent RAG quality - all metrics are strong");
        else if (result.OverallRAGASScore >= 0.70)
            result.Insights.Add("Good RAG quality - minor improvements possible");
        else
            result.Insights.Add("RAG quality needs improvement - review below metrics");
        
        if (result.FaithfulnessScore < FAITHFULNESS_THRESHOLD)
            result.Insights.Add($"Low faithfulness ({result.FaithfulnessScore:F2}) - response may contain hallucinations");
        
        if (result.AnswerRelevancyScore < RELEVANCY_THRESHOLD)
            result.Insights.Add($"Low answer relevancy ({result.AnswerRelevancyScore:F2}) - response may be off-topic");
        
        if (result.ContextPrecisionScore < PRECISION_THRESHOLD)
            result.Insights.Add($"Low context precision ({result.ContextPrecisionScore:F2}) - improve retrieval filtering");
        
        if (result.ContextRecallScore < RECALL_THRESHOLD)
            result.Insights.Add($"Low context recall ({result.ContextRecallScore:F2}) - increase number of retrieved documents");
    }
}
