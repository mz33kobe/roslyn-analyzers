﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeQuality.Analyzers.QualityGuidelines
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AvoidInfiniteRecursion : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2011";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
            new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidInfiniteRecursionTitle), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources)),
            new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidInfiniteRecursionMessageSure), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources)),
            DiagnosticCategory.Reliability,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static DiagnosticDescriptor MaybeRule = new DiagnosticDescriptor(RuleId,
            new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidInfiniteRecursionTitle), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources)),
            new LocalizableResourceString(nameof(MicrosoftCodeQualityAnalyzersResources.AvoidInfiniteRecursionMessageMaybe), MicrosoftCodeQualityAnalyzersResources.ResourceManager, typeof(MicrosoftCodeQualityAnalyzersResources)),
            DiagnosticCategory.Reliability,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, MaybeRule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterOperationBlockStartAction(operationBlockStartContext =>
            {
                if (!(operationBlockStartContext.OwningSymbol is IMethodSymbol methodSymbol) ||
                    methodSymbol.MethodKind != MethodKind.PropertySet)
                {
                    return;
                }

                operationBlockStartContext.RegisterOperationAction(operationContext =>
                {
                    var assignmentOperation = (IAssignmentOperation)operationContext.Operation;

                    if (!(assignmentOperation.Target is IPropertyReferenceOperation operationTarget) ||
                        !(operationTarget.Instance is IInstanceReferenceOperation targetInstanceReference) ||
                        targetInstanceReference.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance ||
                        !operationTarget.Member.Equals(methodSymbol.AssociatedSymbol))
                    {
                        return;
                    }

                    IOperation ancestor = assignmentOperation;
                    do
                    {
                        ancestor = ancestor.Parent;
                    } while (ancestor != null &&
                        ancestor.Kind != OperationKind.AnonymousFunction &&
                        ancestor.Kind != OperationKind.LocalFunction &&
                        ancestor.Kind != OperationKind.Conditional);

                    operationContext.ReportDiagnostic(
                        assignmentOperation.CreateDiagnostic(ancestor != null ? MaybeRule : Rule, operationTarget.Property.Name));
                }, OperationKind.SimpleAssignment);
            });
        }
    }
}
