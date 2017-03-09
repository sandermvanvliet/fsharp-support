﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Daemon.FSharp.Highlightings;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(TypeCheckErrorsStage)}, StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class ResolvedIdentifiersHighlightStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile fsFile, IDaemonProcess process)
    {
      return new ResolvedIdentifiersHighlightProcess(fsFile, process);
    }

    public class ResolvedIdentifiersHighlightProcess : FSharpDaemonStageProcessBase
    {
      private readonly IFSharpFile myFsFile;

      public ResolvedIdentifiersHighlightProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
        : base(process)
      {
        myFsFile = fsFile;
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        myFsFile.Accept(new ResolvedIdentifiersHighlighter(myFsFile, committer, SeldomInterruptChecker));
      }

      private class ResolvedIdentifiersHighlighter : TreeNodeVisitor
      {
        private readonly IFSharpFile myFsFile;
        private readonly Action<DaemonStageResult> myCommitter;
        private readonly SeldomInterruptCheckerWithCheckTime myInterruptChecker;
        private readonly List<HighlightingInfo> myHighlightings;

        public ResolvedIdentifiersHighlighter(IFSharpFile fsFile, Action<DaemonStageResult> committer,
          SeldomInterruptCheckerWithCheckTime interruptChecker)
        {
          myFsFile = fsFile;
          myCommitter = committer;
          myInterruptChecker = interruptChecker;
          myHighlightings = new List<HighlightingInfo>(fsFile.TokenBuffer.Buffer.Length);
        }

        public override void VisitFSharpImplFile(IFSharpImplFile implFile)
        {
          foreach (var module in implFile.Declarations) // todo: implement for signature files
          {
            module.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }
          VisitFSharpFile(implFile);
        }

        public override void VisitFSharpFile(IFSharpFile file)
        {
          foreach (var token in file.Tokens().OfType<FSharpIdentifierToken>())
          {
            var symbol = token.FSharpSymbol;
            if (symbol != null) myHighlightings.Add(CreateHighlighting(token, symbol.GetHighlightingAttributeId()));
            myInterruptChecker.CheckForInterrupt();
          }
          myCommitter(new DaemonStageResult(myHighlightings));
        }


#pragma warning disable 672

        public override void VisitModuleLikeDeclaration(IModuleLikeDeclaration module)
#pragma warning restore 672
        {
          foreach (var member in module.MembersEnumerable)
          {
            member.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }
        }

        public override void VisitOpen(IOpen open)
        {
          var ids = open.LongIdentifier.Identifiers;
          var names = ids.Select(token => token.GetText()).ToArray();
          for (var idsCount = ids.Count; idsCount > 0; idsCount--)
          {
            myInterruptChecker.CheckForInterrupt();
            var entity = ResolveOpenStatement(ids, idsCount, names);
            if (entity == null) continue;

            HighlightResolvedOpenStatement(entity, idsCount, ids);
            return;
          }
        }

        [CanBeNull]
        private FSharpEntity ResolveOpenStatement(TreeNodeCollection<ITokenNode> ids, int idsCount,
          [NotNull] IEnumerable<string> names)
        {
          var lastIdEndOffset = ids[idsCount - 1].GetTreeEndOffset().Offset;
          var namesList = ListModule.OfSeq(names.Take(idsCount));
          return FSharpSymbolsUtil.TryFindFSharpSymbol(myFsFile, namesList, lastIdEndOffset) as FSharpEntity;
        }

        private void HighlightResolvedOpenStatement([NotNull] FSharpEntity entity, int idsCount,
          TreeNodeCollection<ITokenNode> ids)
        {
          // todo: highlight modules as static classes
          for (var i = 0; i < idsCount - 1; i++)
            myHighlightings.Add(CreateHighlighting(ids[i], HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE));

          var highlightingAttrId = entity.IsNamespace
            ? HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE
            : HighlightingAttributeIds.TYPE_STATIC_CLASS_ATTRIBUTE;
          myHighlightings.Add(CreateHighlighting(ids[idsCount - 1], highlightingAttrId));
        }

        private static HighlightingInfo CreateHighlighting(ITreeNode token, string highlightingAttributeId)
        {
          var range = token.GetNavigationRange();
          var highlighting = new FSharpIdentifierHighlighting(highlightingAttributeId, range);
          return new HighlightingInfo(range, highlighting);
        }
      }
    }
  }
}