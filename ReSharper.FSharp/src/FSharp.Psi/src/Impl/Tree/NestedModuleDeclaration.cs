﻿using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedModuleDeclaration
  {
    protected override string DeclaredElementName
    {
      get
      {
        var parentModule = Parent as IModuleLikeDeclaration;
        if (parentModule == null)
          return NameIdentifier.GetModuleCompiledName(Attributes);

        var sourceName = SourceName;
        foreach (var typeDeclaration in parentModule.Children<IFSharpTypeDeclaration>())
          if (typeDeclaration.ShortName == sourceName && typeDeclaration.TypeParameters.IsEmpty)
            return sourceName + "Module";

        return NameIdentifier.GetModuleCompiledName(Attributes);
      }
    }

    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    public bool IsModule => true;
  }
}