namespace PEGStudy.Tests

open System
open Persimmon.Dried
open UseTestNameByReflection
open PEGStudy

module Arb =

  // 今回はサロゲートペアとか考慮しない
  let string =
    let char = { Arb.char with Gen = Gen.alphaNumChar }
    { Arb.string with Gen = (Arb.array char).Gen |> Gen.map String }

module ParserTest =

  open Parser

  let check expected = function
  | Success(actual, _) -> expected = actual
  | _ -> false

  let ``任意の一文字を解釈できる`` = property {
    apply (Prop.forAll Arb.string (fun s ->
      if String.IsNullOrEmpty(s) then run any s = Failure("any: もう入力がないよ")
      else run any s |> check (s.Chars(0))
    )) 
  }

  let ``指定した文字列を解釈できる`` = property {
    apply (Prop.forAll (Arb.string, Arb.string) (fun s t ->
      (not <| String.IsNullOrEmpty(s)) ==> lazy
        run (pstring s) (s + t) |> check s
    )) 
  }
