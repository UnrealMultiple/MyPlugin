﻿using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IDAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IDAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TR0000";
    public const string DiagnosticId2 = "TR0001";
    internal static readonly LocalizableString Title = "Change magic numbers into appropriate ID values";
    internal static readonly LocalizableString MessageFormat = "The number {0} should be changed to {1} for readability";
    internal static readonly LocalizableString Description = "Changes magic numbers into appropriate ID values.";
    internal const string Category = "Design";

    internal static DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, true, Description);
    internal static DiagnosticDescriptor Rule2 = new(DiagnosticId2, Title, MessageFormat, Category, DiagnosticSeverity.Info, true, Description);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule, Rule2];

    public override void Initialize(AnalysisContext context)
    {
        //context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(EqualsExpressionAction, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
        context.RegisterSyntaxNodeAction(SimpleAssignmentExpressionAction, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(InvocationExpressionAction, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(CaseSwitchLabelAction, SyntaxKind.CaseSwitchLabel);
    }
    public static void EqualsExpressionAction(SyntaxNodeAnalysisContext context)
    {
        var node = (BinaryExpressionSyntax)context.Node;
        ExpressionAction(context, node, node.Left, node.Right, Rule);
    }
    public static void SimpleAssignmentExpressionAction(SyntaxNodeAnalysisContext context)
    {
        var node = (AssignmentExpressionSyntax)context.Node;
        ExpressionAction(context, node, node.Left, node.Right, Rule);
    }
    public static void CaseSwitchLabelAction(SyntaxNodeAnalysisContext context)
    {
        var node = (CaseSwitchLabelSyntax)context.Node;
        if(node.Value is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } literalExpression)
        {
            if (node.Parent?.Parent is SwitchStatementSyntax switchStatement)
            {
                ExpressionAction(context, literalExpression, switchStatement.Expression, literalExpression, Rule2);
            }
        }
    }
    public static void InvocationExpressionAction(SyntaxNodeAnalysisContext context)
    {
        var node = (InvocationExpressionSyntax)context.Node;
        if(node.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }
        var fullName = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type!.ToString();
        if (InvocationExpressionReportFilter.TryGetValue(fullName, out var methodFilterInfos))
        {
            IMethodSymbol? methodSymbol = null;
            var methodName = memberAccess.Name.Identifier.ValueText;
            for (int i = 0; i < methodFilterInfos.Length; i++)
            {
                var methodFilterInfo = methodFilterInfos[i];
                if (!methodName.OrdinalEquals(methodFilterInfo.MethodName))
                {
                    continue;
                }
                if (methodFilterInfo.ArgumentCount != -1)
                {
                    methodSymbol ??= context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol as IMethodSymbol;
                    if (methodSymbol is null)
                    {
                        continue;
                    }
                    if (methodSymbol.Parameters.Length != methodFilterInfo.ArgumentCount)
                    {
                        continue;
                    }
                }
                if (node.ArgumentList.Arguments.Count > methodFilterInfo.CheckIndex 
                    && node.ArgumentList.Arguments[methodFilterInfo.CheckIndex] is { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } literalExpression } 
                    && short.TryParse(literalExpression.Token.Text, out var id) && methodFilterInfo.IdToNameDict.TryGetValue(id, out var name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule2, literalExpression.GetLocation(), methodFilterInfo.Properties, id, $"{methodFilterInfo.IdName}.{name}"));
                }
            }
        }
    }
    internal static void ExpressionAction(SyntaxNodeAnalysisContext context, SyntaxNode reportNode, ExpressionSyntax left, ExpressionSyntax right, DiagnosticDescriptor diagnosticDescriptor)
    {
        if (left is MemberAccessExpressionSyntax && right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression })
        {
            var memberemberAccess = (MemberAccessExpressionSyntax)left;
            var literalExpression = (LiteralExpressionSyntax)right;
            var fullName = context.SemanticModel.GetTypeInfo(memberemberAccess.Expression, context.CancellationToken).Type!.ToString();
            if (MemberAccessExpressionReportFilter.TryGetValue(fullName, out var idFilterInfos))
            {
                for (int i = 0; i < idFilterInfos.Length; i++)
                {
                    var idFilterInfo = idFilterInfos[i];
                    if (memberemberAccess.Name.Identifier.ValueText.OrdinalEquals(idFilterInfo.MemberName) && short.TryParse(literalExpression.Token.Text, out var id) && idFilterInfo.IdToNameDict.TryGetValue(id, out var name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, reportNode.GetLocation(), idFilterInfo.Properties, id, $"{idFilterInfo.IdName}.{name}"));
                    }
                }
            }
        }
        else if (left is ElementAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } }] })
        {
            var literalExpression = (LiteralExpressionSyntax)((ElementAccessExpressionSyntax)left).ArgumentList.Arguments[0].Expression;
            var fullName = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type!.ToString();
            if (ElementAccessExpressionReportFilter.TryGetValue(fullName, out var idFilterInfos))
            {
                for (int i = 0; i < idFilterInfos.Length; i++)
                {
                    var idFilterInfo = idFilterInfos[i];
                    if (memberAccess.Name.Identifier.ValueText.OrdinalEquals(idFilterInfo.MemberName) && short.TryParse(literalExpression.Token.Text, out var id) && idFilterInfo.IdToNameDict.TryGetValue(id, out var name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule2, literalExpression.GetLocation(), idFilterInfo.Properties, id, $"{idFilterInfo.IdName}.{name}"));
                    }
                }
            }
        }
    }

    internal static FrozenDictionary<string, IdFilterInfo[]> MemberAccessExpressionReportFilter;
    internal static FrozenDictionary<string, IdFilterInfo[]> ElementAccessExpressionReportFilter;
    public static FrozenDictionary<string, MethodFilterInfo[]> InvocationExpressionReportFilter;

    internal static FrozenDictionary<short, string> NPCID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> NPCIDType;

    internal static FrozenDictionary<short, string> ItemID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> ItemIDType;

    internal static FrozenDictionary<short, string> TileID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> TileIDType;

    internal static FrozenDictionary<short, string> WallID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> WallIDType;

    internal static FrozenDictionary<short, string> MessageID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> MessageIDType;

    internal static FrozenDictionary<short, string> InvasionID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> InvasionIDType;

    internal static FrozenDictionary<short, string> ProjectileID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> ProjectileIDType;

    internal static FrozenDictionary<short, string> PlayerDifficultyID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> PlayerDifficultyIDType;

    internal static FrozenDictionary<short, string> SoundID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> SoundIDType;

    internal static FrozenDictionary<short, string> BuffID = IDs.GetInt16ID();
    internal static ImmutableDictionary<string, string?> BuffIDType;

    internal static FrozenDictionary<string, FrozenDictionary<short, string>> IDsDict;
    static IDAnalyzer()
    {
        MemberAccessExpressionReportFilter = new Dictionary<string, IdFilterInfo[]>()
        {
            { "Terraria.NPC", [GetIdFilterInfo("type", nameof(IDs.NPCID))] },
            { "Terraria.Item", [GetIdFilterInfo("type", nameof(IDs.ItemID))] },
            { "Terraria.ITile", [GetIdFilterInfo("type", nameof(IDs.TileID)), GetIdFilterInfo("wall", nameof(IDs.WallID))] },
            { "Terraria.Projectile", [GetIdFilterInfo("type", nameof(IDs.ProjectileID))] },
            { "Terraria.Main", [GetIdFilterInfo("invasionType", nameof(IDs.InvasionID))] },
            { "Terraria.Player", [GetIdFilterInfo("difficulty", nameof(IDs.PlayerDifficultyID))] },
        }.ToFrozenDictionary(StringComparer.Ordinal);
        ElementAccessExpressionReportFilter = new Dictionary<string, IdFilterInfo[]>()
        {
            { "Terraria.Main", [GetIdFilterInfo("townNPCCanSpawn", nameof(IDs.NPCID))] },
            { "Terraria.NPC", [
                GetIdFilterInfo("buffImmune", nameof(IDs.BuffID))
            ] },
            { "Terraria.Player", [
                GetIdFilterInfo("buffImmune", nameof(IDs.BuffID))
            ] }
        }.ToFrozenDictionary(StringComparer.Ordinal);

        InvocationExpressionReportFilter = new Dictionary<string, MethodFilterInfo[]>()
        {
            { "Terraria.NPC", [
                GetMethodFilterInfo("AnyNPCs", nameof(NPCID), 0),
                GetMethodFilterInfo("CountNPCS", nameof(NPCID), 0),
                GetMethodFilterInfo("FindFirstNPC", nameof(NPCID), 0),
                GetMethodFilterInfo("SpawnOnPlayer", nameof(NPCID), 1),
                GetMethodFilterInfo("SpawnBoss", nameof(NPCID), 2),
                GetMethodFilterInfo("MechSpawn", nameof(NPCID), 2),
                GetMethodFilterInfo("NewNPC", nameof(NPCID), 3..),
                GetMethodFilterInfo("SetDefaults", nameof(NPCID), 0..),
            ] },
            { "Terraria.Item", [
                GetMethodFilterInfo("NewItem", nameof(ItemID), 3, 9),
                GetMethodFilterInfo("NewItem", nameof(ItemID), 5, 11),
                GetMethodFilterInfo("SetDefaults", nameof(ItemID), 0..),
            ] },
            { "Terraria.Projectile", [
                GetMethodFilterInfo("NewProjectile", nameof(ProjectileID), 3, 10),
                GetMethodFilterInfo("NewProjectile", nameof(ProjectileID), 5, 12),
            ] },
            { "Terraria.NetMessage", [
                GetMethodFilterInfo("SendData", nameof(MessageID), 0..),
                GetMethodFilterInfo("TrySendData", nameof(MessageID), 0..),
            ] },
            { "Terraria.Audio.SoundEngine", [
                GetMethodFilterInfo("PlaySound", nameof(SoundID), 0..),
            ] },
            { "Terraria.TileObject", [
                GetMethodFilterInfo("CanPlace", nameof(TileID), 2..)
            ] },
            { "Terraria.Player", [
                GetMethodFilterInfo("IsTileTypeInInteractionRange", nameof(TileID), 0..),
                GetMethodFilterInfo("isNearNPC", nameof(NPCID), 0),
                GetMethodFilterInfo("AddBuff", nameof(BuffID), 0)
            ] }
        }.ToFrozenDictionary(StringComparer.Ordinal);

        NPCIDType = AddType(nameof(NPCID));
        ItemIDType = AddType(nameof(ItemID));
        TileIDType = AddType(nameof(TileID));
        WallIDType = AddType(nameof(WallID));
        MessageIDType = AddType(nameof(MessageID));
        InvasionIDType = AddType(nameof(InvasionID));
        ProjectileIDType = AddType(nameof(ProjectileID));
        PlayerDifficultyIDType = AddType(nameof(PlayerDifficultyID));
        SoundIDType = AddType(nameof(SoundID));
        BuffIDType = AddType(nameof(BuffID));

        IDsDict = new Dictionary<string, FrozenDictionary<short, string>>()
        {
            { nameof(NPCID), NPCID },
            { nameof(ItemID), ItemID },
            { nameof(TileID), TileID },
            { nameof(WallID), WallID },
            { nameof(MessageID), MessageID },
            { nameof(InvasionID), InvasionID },
            { nameof(ProjectileID), ProjectileID },
            { nameof(PlayerDifficultyID), PlayerDifficultyID },
            { nameof(SoundID), SoundID },
            { nameof(BuffID), BuffID }
        }.ToFrozenDictionary(StringComparer.Ordinal);
    }
    private static IdFilterInfo GetIdFilterInfo(string memberName, string idName) => new(memberName, IDs.GetInt16ID(idName), AddType(idName), idName);
    private static MethodFilterInfo GetMethodFilterInfo(string methodName, string idName, int checkIndex, int argumentCount) => new(methodName, checkIndex, argumentCount, IDs.GetInt16ID(idName), AddType(idName), idName);
    private static MethodFilterInfo GetMethodFilterInfo(string methodName, string idName, int checkIndex) => new(methodName, checkIndex, checkIndex + 1, IDs.GetInt16ID(idName), AddType(idName), idName);
    private static MethodFilterInfo GetMethodFilterInfo(string methodName, string idName, Range range) => new(methodName, range.Start.Value, -1, IDs.GetInt16ID(idName), AddType(idName), idName);
    private static ImmutableDictionary<string, string?> AddType(string type)
    {
        const string key = "type";
        var builder = ImmutableDictionary.CreateBuilder<string, string?>(StringComparer.Ordinal);
        builder[key] = type;
        return builder.ToImmutable();
    }
}

internal sealed class IdFilterInfo(string memberName, FrozenDictionary<short, string> idToNameDict, ImmutableDictionary<string, string?> properties, string idName)
{
    public string MemberName = memberName;
    public FrozenDictionary<short, string> IdToNameDict = idToNameDict;
    public ImmutableDictionary<string, string?> Properties = properties;
    public string IdName = idName;
}

public sealed class MethodFilterInfo
{
    public string MethodName;
    public int ArgumentCount;
    public int CheckIndex;
    public FrozenDictionary<short, string> IdToNameDict;
    public ImmutableDictionary<string, string?> Properties;
    public string IdName;

    public MethodFilterInfo(string methodName, int checkIndex, int argumentCount, FrozenDictionary<short, string> idToNameDict, ImmutableDictionary<string, string?> properties, string idName)
    {
        MethodName = methodName;
        if (argumentCount < 0)
        {
            argumentCount = -1;
        }
        if (argumentCount != -1 && checkIndex >= argumentCount)
        {
            throw new ArgumentException("checkIndex >= argumentCount");
        }
        ArgumentCount = argumentCount;
        CheckIndex = checkIndex;
        IdToNameDict = idToNameDict;
        Properties = properties;
        IdName = idName;
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IDAnalyzerCodeFixProvider)), Shared]
public sealed class IDAnalyzerCodeFixProvider : CodeFixProvider
{
    private const string Title = "Change magic number into appropriate ID value";
    public sealed override ImmutableArray<string> FixableDiagnosticIds => [IDAnalyzer.DiagnosticId, IDAnalyzer.DiagnosticId2];
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if(root is null)
        {
            return;
        }
        var diagnostic = context.Diagnostics.First();
        var type = diagnostic.Properties["type"]!;
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        if (diagnostic.Id == IDAnalyzer.DiagnosticId)
        {
            foreach (var declaration in root.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf())
            {
                if (declaration is AssignmentExpressionSyntax { Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } literalExpression })
                {
                    context.RegisterCodeFix(CodeAction.Create(Title, c => ReplaceNodeAsync(type, context.Document, literalExpression, c), Title), diagnostic);
                }
                else if (declaration is BinaryExpressionSyntax { Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } literalExpression2 })
                {
                    context.RegisterCodeFix(CodeAction.Create(Title, c => ReplaceNodeAsync(type, context.Document, literalExpression2, c), Title), diagnostic);
                }
            }
        }
        else if(diagnostic.Id == IDAnalyzer.DiagnosticId2)
        {
            foreach (var declaration in root.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf())
            {
                if (declaration is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression })
                {
                    context.RegisterCodeFix(CodeAction.Create(Title, c => ReplaceNodeAsync(type, context.Document, (LiteralExpressionSyntax)declaration, c), Title), diagnostic);
                }
                else if(declaration is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.UnaryMinusExpression })
                {
                    context.RegisterCodeFix(CodeAction.Create(Title, c => ReplaceNodeAsync(type, context.Document, (PrefixUnaryExpressionSyntax)declaration, c), Title), diagnostic);
                }
            }
        }
    }
    private static Task<Document> ReplaceNodeAsync(string type, Document document, LiteralExpressionSyntax literalExpression, CancellationToken cancellationToken)
    {
        var root = document.GetSyntaxRootAsync(cancellationToken).Result!;
        SyntaxNode newRoot = root.ReplaceNode(literalExpression, SyntaxFactory.IdentifierName(SyntaxFactory.Identifier($"{type}.{IDAnalyzer.IDsDict[type][short.Parse(literalExpression.Token.Text)]}")))!;
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
    private static Task<Document> ReplaceNodeAsync(string type, Document document, PrefixUnaryExpressionSyntax prefixUnaryExpression, CancellationToken cancellationToken)
    {
        var root = document.GetSyntaxRootAsync(cancellationToken).Result!;
        SyntaxNode newRoot = root.ReplaceNode(prefixUnaryExpression, SyntaxFactory.IdentifierName(SyntaxFactory.Identifier($"{type}.{IDAnalyzer.IDsDict[type][short.Parse(prefixUnaryExpression.Parent!.GetText().ToString())]}")))!;
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}