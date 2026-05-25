using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ProjectAscendant.Analyzers
{
    /// <summary>
    /// PA0001 — Balance Literal in Non-ConfigSO File.
    ///
    /// All numeric balance values (damage multipliers, stat thresholds, economy constants,
    /// AP costs, XP curve parameters, etc.) must live in a *ConfigSO ScriptableObject field.
    /// Hardcoding them in game logic violates the data-driven pillar of the architecture.
    ///
    /// Per Project Ascendant coding-standards.md and CLAUDE.md:
    ///   "All content must be ScriptableObject-driven — no hardcoded game values."
    ///
    /// Fires on: float/double literals that are not 0, 1, or -1 (structural constants).
    /// Exempt files: *ConfigSO.cs, Assets/Scripts/Editor/**, Assets/Tests/**
    ///
    /// Suppress with: // PA0001-suppress on the offending line (for intentional structural uses).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BalanceLiteralAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PA0001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Balance literal in non-ConfigSO file",
            messageFormat: "Numeric literal '{0}' looks like a balance value. " +
                           "Move it to a *ConfigSO ScriptableObject field " +
                           "(per Project Ascendant data-discipline rules).",
            category: "ProjectAscendant.DataDiscipline",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:
                "All balance values must live in *ConfigSO ScriptableObjects. " +
                "This diagnostic fires on float/double literals outside ConfigSO, Editor, and Test files. " +
                "Structural constants (0, 1, -1) are exempt. " +
                "Suppress with a '// PA0001-suppress' comment on the same line if intentional.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeLiteral, SyntaxKind.NumericLiteralExpression);
        }

        // ── Core check ─────────────────────────────────────────────────────
        private static void AnalyzeLiteral(SyntaxNodeAnalysisContext context)
        {
            string filePath = context.Node.SyntaxTree.FilePath.Replace('\\', '/');

            // Skip files that are allowed to contain balance literals
            if (IsExemptFile(filePath))
                return;

            var literal = (LiteralExpressionSyntax)context.Node;

            // Skip if the line has an explicit suppression comment
            if (HasSuppressComment(literal))
                return;

            object? value = literal.Token.Value;

            // Only flag float/double literals — these are the classic balance-value type.
            // Integer literals are too noisy (array indices, loop bounds, etc.).
            if (value is float f)
            {
                if (IsStructuralConstant(f)) return;
                Report(context, literal);
            }
            else if (value is double d)
            {
                if (IsStructuralConstant(d)) return;
                Report(context, literal);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static bool IsExemptFile(string filePath)
        {
            // *ConfigSO.cs — the canonical home for all balance values
            if (filePath.EndsWith("ConfigSO.cs", StringComparison.OrdinalIgnoreCase))
                return true;

            // Editor tooling scripts (seeders, inspectors, validators, importers)
            if (filePath.Contains("/Editor/", StringComparison.OrdinalIgnoreCase))
                return true;

            // Unit and integration test files
            if (filePath.Contains("/Tests/", StringComparison.OrdinalIgnoreCase))
                return true;
            if (filePath.Contains("/EditMode/", StringComparison.OrdinalIgnoreCase))
                return true;
            if (filePath.Contains("/PlayMode/", StringComparison.OrdinalIgnoreCase))
                return true;

            // GameTypes.cs — pure enum/struct definitions, no balance values but no SO ref either
            if (filePath.EndsWith("GameTypes.cs", StringComparison.OrdinalIgnoreCase))
                return true;

            // Analysis project itself (this file)
            if (filePath.Contains("/Analysis/", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Structural constants that are not balance values.
        /// 0 = none/empty, 1 = identity multiplier, -1 = invert/negate.
        /// </summary>
        private static bool IsStructuralConstant(float value) =>
            value == 0f || value == 1f || value == -1f;

        private static bool IsStructuralConstant(double value) =>
            value == 0.0 || value == 1.0 || value == -1.0;

        /// <summary>
        /// Checks if the literal's line contains a PA0001-suppress comment.
        /// This allows intentional structural floats that don't fit the 0/1/-1 exemption.
        /// Example: float t = 0.5f; // PA0001-suppress — normalised lerp parameter
        /// </summary>
        private static bool HasSuppressComment(LiteralExpressionSyntax literal)
        {
            // Walk trailing trivia on the literal's parent statement for a suppress comment
            var line = literal.SyntaxTree.GetText().Lines
                .GetLineFromPosition(literal.SpanStart);
            string lineText = line.ToString();
            return lineText.Contains("PA0001-suppress", StringComparison.Ordinal);
        }

        private static void Report(SyntaxNodeAnalysisContext context,
                                    LiteralExpressionSyntax literal)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                literal.GetLocation(),
                literal.Token.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
