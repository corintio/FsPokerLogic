﻿module ImportTests

open Hands
open Preflop
open Import
open Xunit
open FsCheck
open FsCheck.Xunit

let fileNameIP = System.IO.Directory.GetCurrentDirectory() + @"\IPinput.xlsx"
let rulesIP = importRuleFromExcel importRulesIP fileNameIP |> List.ofSeq
let decideIP = decideOnRules rulesIP

type BigBets =
    static member Int() =
        Arb.Default.Decimal()
        |> Arb.filter (fun c -> 1m <= c && c <= 25m)

[<Property(Arbitrary = [| typeof<BigBets> |])>]
let ``decide on IP import has value for any stack and hand`` bb h =
  let action = decideIP bb [] h
  not (action = None)

let decideOnImported decide handString stack history expected =
  let hand = parseHand handString
  let actual = decide stack history hand
  Assert.Equal (
    (match expected with
      | "AllIn" -> AllIn
      | "Raise" -> MinRaise
      | "RaiseX2.5" -> RaiseX 2.5m
      | "RaiseX3" -> RaiseX 3m
      | "Check" -> ActionPattern.Check
      | "Call" -> Call
      | "Fold" -> Fold
      | _ -> failwith "Unknown expected"
      |> Some),
      actual)

[<Theory>]
[<InlineData("Q4o", 25, "Fold")>]
[<InlineData("KTo", 24, "Call")>]
[<InlineData("AA", 23, "Raise")>]
[<InlineData("22", 22, "AllIn")>]
[<InlineData("T9o", 12, "AllIn")>]
[<InlineData("T7s", 11, "Fold")>]
let ``decide on imported / open`` handString stack expected =
  decideOnImported decideIP handString stack [] expected

[<Theory>]
[<InlineData("98o", 21, 2.5, "Fold")>]
[<InlineData("98s", 20, 2.5, "Call")>]
[<InlineData("KQo", 21, 2.5, "AllIn")>]
[<InlineData("KTs", 19, 3.5, "Fold")>]
[<InlineData("KJs", 20, 3.5, "AllIn")>]
let ``decide on imported / limp raise`` handString stack raiseSize expected =
  decideOnImported decideIP handString stack [Limp; Raise(raiseSize, raiseSize)] expected

[<Theory>]
[<InlineData("KJo", 25, "Fold")>]
[<InlineData("KQs", 24, "Call")>]
let ``decide on imported / limp allin`` handString stack expected =
  decideOnImported decideIP handString stack [Limp; RaiseAllIn] expected

[<Theory>]
[<InlineData("95s", 18, 3.2, "Fold")>]
[<InlineData("T5s", 17, 3.2, "Call")>]
[<InlineData("KK", 16, 3.2, "RaiseX2.5")>]
[<InlineData("A5o", 15, 3.2, "AllIn")>]
[<InlineData("T5s", 14, 3.9, "Fold")>]
[<InlineData("T7o", 13, 3.9, "Call")>]
[<InlineData("A6s", 25, 3.9, "AllIn")>]
[<InlineData("K7s", 24, 5.5, "Fold")>]
[<InlineData("76s", 23, 5.5, "Call")>]
[<InlineData("A8o", 22, 5.5, "AllIn")>]
[<InlineData("K9s", 21, 7.1, "Fold")>]
[<InlineData("AKs", 20, 7.1, "AllIn")>]
let ``decide on imported / reraise`` handString stack raiseSize expected =
  decideOnImported decideIP handString stack [Raise(2m, 2m); Raise(raiseSize, raiseSize)] expected

[<Theory>]
[<InlineData("A7s", 19, "Fold")>]
[<InlineData("99", 18, "Call")>]
let ``decide on imported / raise allin`` handString stack expected =
  decideOnImported decideIP handString stack [Raise(2m, 2m); RaiseAllIn] expected

let fileNameOOP = System.IO.Directory.GetCurrentDirectory() + @"\OOPinput.xlsx"
let rulesOOP = importRuleFromExcel importRulesOOP fileNameOOP |> List.ofSeq
let decideOOP = decideOnRules rulesOOP

[<Property(Arbitrary = [| typeof<BigBets> |])>]
let ``decide on OOP import has value for any stack and hand`` bb history h =
  let action = decideOOP bb [history] h
  Assert.NotEqual(None, action)

[<Theory>]
[<InlineData("QQ", 25, 2.3, "AllIn")>]
[<InlineData("JJ", 25, 2.3, "Fold")>]
[<InlineData("AA", 25, 2.7, "AllIn")>]
[<InlineData("JJ", 25, 2.7, "Fold")>]
let ``decide on imported / 4bet`` handString stack raiseSize expected =
  decideOnImported decideOOP handString stack [Raise(raiseSize, raiseSize); Raise(2.5m, 2.5m); Raise(2m, 2m)] expected

[<Theory>]
[<InlineData("JJ", 25, 2.3, "Call")>]
[<InlineData("T3s", 25, 2.3, "Fold")>]
[<InlineData("KK", 25, 2.7, "Call")>]
[<InlineData("T2s", 25, 2.7, "Fold")>]
let ``decide on imported / 4bet allin`` handString stack raiseSize expected =
  decideOnImported decideOOP handString stack [Raise(raiseSize, raiseSize); Raise(2.5m, 2.5m); RaiseAllIn] expected

[<Theory>]
[<InlineData("Q9s", 7, "Check")>]
[<InlineData("JTo", 6, "RaiseX3")>]
[<InlineData("22", 5, "AllIn")>]
let ``decide on imported / limp`` handString stack expected =
  decideOnImported decideOOP handString stack [Limp] expected

[<Theory>]
[<InlineData("94s", 4, "Fold")>]
[<InlineData("Q2s", 3, "Call")>]
[<InlineData("88", 2, "RaiseX2.5")>]
[<InlineData("66", 1, "AllIn")>]
let ``decide on imported / raise`` handString stack expected =
  decideOnImported decideOOP handString stack [Raise(2.5m, 2.5m)] expected

[<Theory>]
[<InlineData("K6s", 7, "Fold")>]
[<InlineData("K7s", 6, "Call")>]  
let ``decide on imported / allin`` handString stack expected =
  decideOnImported decideOOP handString stack [RaiseAllIn] expected

[<Theory>]
[<InlineData("QTo", 5, "Fold")>]
[<InlineData("QJo", 4, "Call")>]
[<InlineData("AJo", 3, "AllIn")>]
let ``decide on imported / limp raise reraise`` handString stack expected =
  decideOnImported decideOOP handString stack [Limp; Raise(3m, 3m); Raise(5m, 5m)] expected

[<Theory>]
[<InlineData("QTs", 2, "Fold")>]
[<InlineData("KQo", 1, "Call")>]
let ``decide on imported / limp raise allin`` handString stack expected =
  decideOnImported decideOOP handString stack [Limp; Raise(3m, 3m); RaiseAllIn] expected

[<Fact>]
let ``decide on imported / allin on 4bet`` () =
  decideOnImported decideOOP "88" 6.66m [Raise(2m, 2m); Raise(4m, 4m); Raise(6m, 6m)] "AllIn"

[<Fact>]
let ``decide on imported / call 4bet allin`` () =
  decideOnImported decideOOP "99" 6.66m [Raise(2m, 2m); Raise(4m, 4m); RaiseAllIn] "Call"