﻿using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpNamespaceDeclaration
  {
    public override string DeclaredName => QualifiedName;
    public string QualifiedName => LongIdentifier.QualifiedName;
    public override string ShortName => LongIdentifier.Name;
    public bool IsModule => false;

    public override TreeTextRange GetNameRange()
    {
      return LongIdentifier.GetNameRange();
    }

    public DocumentRange GetDeclaredNameDocumentRange()
    {
      return LongIdentifier.GetDocumentRange();
    }

    protected override void PreInit()
    {
      base.PreInit();
      CacheDeclaredElement = null;
    }

    public override IDeclaredElement DeclaredElement
    {
      get
      {
        Assertion.Assert(IsValid(), "Getting declared element from invalid declaration");
        Assertion.Assert(CacheDeclaredElement == null || CacheDeclaredElement.IsValid(),
          "myCacheDeclaredElement == null || myCacheDeclaredElement.IsValid()");
        return CacheDeclaredElement;
      }
    }

    public IDeclaredElement CacheDeclaredElement { get; set; }

    INamespace INamespaceDeclaration.DeclaredElement => DeclaredElement as INamespace;

    public override void SetName(string name)
    {
      // todo
    }

    public void SetQualifiedName(string qualifiedName)
    {
      // todo
    }
  }
}