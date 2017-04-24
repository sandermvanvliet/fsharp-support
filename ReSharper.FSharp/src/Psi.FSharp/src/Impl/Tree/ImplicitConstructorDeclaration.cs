﻿using System.Linq;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class ImplicitConstructorDeclaration
  {
    public override string DeclaredName =>
      GetContainingTypeDeclaration()?.DeclaredName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName =>
      GetContainingTypeDeclaration()?.SourceName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetNameRange()
    {
      return GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var entity = GetFSharpSymbol() as FSharpEntity;
      var ctor = entity?.MembersFunctionsAndValues.FirstOrDefault(m => m.IsImplicitConstructor);
      return ctor != null ? new FSharpImplicitConstructor(this, ctor) : null;
    }
  }
}