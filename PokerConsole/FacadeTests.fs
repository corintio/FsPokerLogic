﻿module FacadeTests

open Cards
open Hands
open Actions
open Xunit
open Preflop.Decide
open Excel
open Import
open Preflop.Facade

let fileNameIP = System.IO.Directory.GetCurrentDirectory() + @"\IPinput.xlsx"
let rulesIP = importExcel (importRulesByStack importRulesIP) fileNameIP
let fileNameOOP = System.IO.Directory.GetCurrentDirectory() + @"\OOPinput.xlsx"
let rulesOOP = importExcel (importRulesByStack importRulesOOP) fileNameOOP
let fileNameAdvancedOOP = System.IO.Directory.GetCurrentDirectory() + @"\PostflopPART2.xlsx"
let rulesAdvancedOOP = importExcel importOopAdvanced fileNameAdvancedOOP
let rules = List.concat [rulesIP; rulesAdvancedOOP.Always; rulesAdvancedOOP.LimpFoldLow; rulesOOP]

let test s vb hb hand history openRange expected =
  let bb = 20
  let hs = s - hb
  let vs = 1000 - s - vb
  let stack = min (hs + hb) (vs + vb)
  let effectiveStack = decimal stack / decimal bb
  let callSize = min (vb - hb) hs
  let potOdds = (callSize |> decimal) * 100m / (vb + hb + callSize |> decimal) |> ceil |> int
  let fullHand = parseFullHand hand
  let result = decideOnRules rules effectiveStack potOdds openRange history fullHand
  Assert.Equal(Some expected, result)

let testVsPfr vb hand openRange = test 500 vb 20 hand [WasRaise(decimal(vb) / 20m)] openRange AllIn

[<Fact>]
let ``3bet allin for 2.5 raise based on old rules`` () =
  testVsPfr 50 "AhKh" 60m

[<Fact>]
let ``3bet allin for 2 raise based on 3b shove rules with stats on the edge of two rows`` () =
  testVsPfr 40 "Ad8c" 48m

[<Fact>]
let ``3bet allin for 5x raise based on old rules`` () =
  testVsPfr 100 "Js8c" 60m

[<Fact>]
let ``3bet for bluff`` () =
  test 500 40 20 "5c6d" [WasRaise 2m] 78m (RaiseBluffX 2.5m)

[<Fact>]
let ``push AI with 4bb`` () =
  test 80 15 20 "TcTd" [] 0m AllIn

[<Fact>]
let ``call 4b AI`` () =
  test 400 400 100 "TsTh" [WasRaise 3m; WasRaise 5m; WasRaiseAllIn] 0m Call

let beaversFileName = System.IO.Directory.GetCurrentDirectory() + @"\mfck beavers.xlsx"

[<Fact>]
let ``decidePre vs regs (beavers) - AI OOP vs PFR`` () =
  use xl = useExcel beaversFileName
  let s = { BB = 20; HeroStack = 440; HeroBet = 20; HeroHand = parseFullHand "As2c"; VillainStack = 490; VillainBet = 50; VillainName = "angry_bird" }
  let h = [WasRaise 2.5m]
  let result = decidePre (xl.Workbook) [] [] [] ["somebody"; "angry_bird"] s h
  Assert.Equal(Some (ActionPattern.AllIn, "mfck beavers -> OOP 23bb shove -> D26"), result)

[<Fact>]
let ``decidePre vs regs (beavers) - fold OOP vs PFR`` () =
  use xl = useExcel beaversFileName
  let s = { BB = 20; HeroStack = 440; HeroBet = 20; HeroHand = parseFullHand "Ks2c"; VillainStack = 490; VillainBet = 50; VillainName = "angry_bird" }
  let h = [WasRaise 2.5m]
  let result = decidePre (xl.Workbook) [] [] [] ["somebody"; "angry_bird"] s h
  Assert.Equal(Some (ActionPattern.Fold, "mfck beavers -> OOP 23bb shove -> D26"), result)

[<Fact>]
let ``decidePreRegwar vs not-regs - returns None`` () =
  use xl = useExcel beaversFileName
  let s = { BB = 20; HeroStack = 440; HeroBet = 20; HeroHand = parseFullHand "As2c"; VillainStack = 490; VillainBet = 50; VillainName = "notreg1" }
  let h = [WasRaise 2.5m]
  let result = decidePreRegwar (xl.Workbook) ["reg1"; "reg2"] s h ()
  Assert.Equal(None, result)