module Module

Ns2.Foo(Ns1.Bar.Nested()) |> ignore

open Ns1
open Ns2

Foo(Bar.Nested()) |> ignore
