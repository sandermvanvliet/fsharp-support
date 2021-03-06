namespace rec JetBrains.ReSharper.Plugins.FSharp.Services.Foldings

open JetBrains.DocumentModel
open JetBrains.ReSharper.Daemon.CodeFolding
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.TextControl.DocumentMarkup
open JetBrains.Util
open JetBrains.Util.Logging
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.Structure

[<Language(typeof<FSharpLanguage>)>]
type FSharpCodeFoldingProcessFactory() =
    interface ICodeFoldingProcessorFactory with
        member x.CreateProcessor() =
            FSharpCodeFoldingProcess() :> _


and FSharpCodeFoldingProcess(logger) =
    inherit TreeNodeVisitor<IHighlightingConsumer>()
    let mutable processingFinished = false

    let getFoldingAttrId = function
        | Scope.Open -> CodeFoldingAttributes.IMPORTS_FOLDING_ATTRIBUTE
        | Scope.Attribute -> CodeFoldingAttributes.ATTRIBUTES_FOLDING_ATTRIBUTE
        | Scope.Comment -> CodeFoldingAttributes.COMMENTS_FOLDING_ATTRIBUTE
        | Scope.XmlDocComment -> CodeFoldingAttributes.DOCUMENTATION_COMMENTS_FOLDING_ATTRIBUTE
        | Scope.Member -> CodeFoldingAttributes.METHOD_FOLDING_ATTRIBUTE
        | Scope.HashDirective -> FSharpCodeFoldingAttributes.HashDirective
        | _ -> CodeFoldingAttributes.DEFAULT_FOLDING_ATTRIBUTE

    override x.VisitNode(element, context) =
        match element.GetContainingNode<IFSharpFile>() with
        | null -> ()
        | fsFile ->

        match fsFile.ParseTree with
        | None -> ()
        | Some parseTree ->

        let sourcefile = element.GetSourceFile()
        let document = sourcefile.Document
        let lines = [| for line in 0 .. (int (document.GetLineCount().Minus1())) do
                        yield document.GetLineText(docLine line) |]

        Structure.getOutliningRanges lines parseTree
        |> Seq.distinctBy (fun x -> x.Range.StartLine)
        |> Seq.iter (fun x ->
            let mutable textRange = x.CollapseRange.ToTextRange(document)
            let docRange = DocumentRange(document, textRange)
            let placeholder =
                match x.Scope with
                | Scope.Open -> "..."
                | _ ->

                let line = (docLine x.CollapseRange.StartLine).Minus1()
                let lineStart = document.GetLineStartOffset(line)
                let lineEnd = document.GetLineEndOffsetNoLineBreak(line)
                match TextRange(lineStart, lineEnd).Intersect(&textRange) with
                | range when not range.IsEmpty -> document.GetText(range) + " ..."
                | _ -> " ..."

            if not textRange.IsEmpty then
                let highlighting = CodeFoldingHighlighting(getFoldingAttrId x.Scope, placeholder, docRange, 0)
                context.AddHighlighting(highlighting)
            else
                let logger = Logger.GetLogger<FSharpCodeFoldingProcess>()
                logger.LogMessage(LoggingLevel.WARN, sprintf "Empty folding: %O %A" textRange x))

    interface ICodeFoldingProcessor with
        member x.InteriorShouldBeProcessed(element, context) = false
        member x.IsProcessingFinished(context) = processingFinished
        member x.ProcessAfterInterior(element, context) = ()
        member x.ProcessBeforeInterior(element, context) =
            processingFinished <- true
            match element with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()


module FSharpCodeFoldingAttributes =
    let [<Literal>] HashDirective = "ReSharper F# Hash Directives Block Folding"


[<assembly: RegisterHighlighter(FSharpCodeFoldingAttributes.HashDirective, EffectType = EffectType.FOLDING)>]
do ()
