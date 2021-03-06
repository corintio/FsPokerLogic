﻿namespace Preflop

open Hands
open Ranges

module Decide =

  type ActionPattern = 
    | AllIn
    | MinRaise
    | RaiseX of decimal
    | RaiseBluffX of decimal
    | Call
    | Check
    | Fold

  type RelativePosition = IP | OOP

  type HistoryItem = 
    | WasLimp
    | WasRaise of decimal
    | WasRaiseAllIn

  type RuleHistoryItem = 
    | Limp
    | Raise of decimal * decimal
    | RaiseEQ of int
    | RaiseFor3BetShove of decimal * decimal // calling range * opening range threshold
    | BluffableRaise
    | RaiseAllIn

  type DecisionRule = 
    { StackRange : int * int
      Range : string
      History : RuleHistoryItem seq
      Action : ActionPattern
      Source: string }

  type VillainStats =
    { VillainName: string
      OpenRaise20_25: int
      OpenRaise16_19: int
      OpenRaise14_15: int
      LimpFold: int }

  let hud (data: VillainStats list) villainName = 
    let fuzzyNameMatch (parsed: string) full = 
      if parsed = full then true
      else
        let parsedPart = parsed.Replace("?", "")
        parsedPart.Length >= 7 && full.IndexOf(parsedPart) > 0
    let matching = data |> List.tryFind (fun s -> fuzzyNameMatch villainName s.VillainName)
    defaultArg matching (List.head data)

  let isHistoryMatching ranges history stack odds openingRange =
    let if3BetShove raiseX callingRange openingRange allinPot =
      if openingRange < 0.01m then 100m
      else
        (((-((((raiseX+1m))*((100m-((100m*callingRange)/openingRange))/100m))))*(openingRange/callingRange)+((allinPot/2m)-1m))*100m)/allinPot

    if Seq.length ranges = Seq.length history 
    then
      Seq.zip ranges history
      |> Seq.forall (fun (r, h) -> 
        match (r, h) with
        | Limp, WasLimp -> true
        | RaiseAllIn, WasRaiseAllIn -> true
        | Raise (min, max), WasRaise v -> min <= v && v <= max
        | RaiseEQ eq, WasRaise _ -> eq >= odds
        | RaiseFor3BetShove(cra, orathres), WasRaise v when v <= 2.49m -> 
          let k = if3BetShove v cra openingRange (stack*2m)
          (orathres >= 45m || k <= orathres) && (orathres <= 27m || k > orathres - 0.5m) // 27 is min and 45 is max defined
        | BluffableRaise, WasRaise v -> openingRange > 60m && v = 2m
        | _ -> false)
    else false

  let decideOnRules rules stack odds openingRange history h = 
    let isMatching rule h = 
      let ranges = parseRanges rule.Range
      let (stackMin, stackMax) = rule.StackRange
      let (stackMinF, stackMaxF) = (stackMin |> decimal, stackMax |> decimal)
      isHandInRanges ranges h 
        && stackMinF - 0.5m <= stack 
        && stack <= stackMaxF + 0.5m 
        && (isHistoryMatching rule.History history stack odds openingRange)
    let findMatching rs =
      rs
      |> Seq.filter (fun r -> isMatching r (normalize h))
      |> Seq.tryHead 
      |> Option.map (fun x -> x.Action, x.Source)

    let (nonFoldRules, foldRules) = rules |> List.partition (fun x -> x.Action <> Fold)
    let nonFold = findMatching nonFoldRules
    if nonFold.IsSome then nonFold
    else findMatching foldRules

  type PreSnapshot = {
    BB: int
    HeroStack: int
    HeroBet: int
    HeroHand: Hand
    VillainStack: int
    VillainBet: int
    VillainName: string
  }

  let decidePreStandard rulesBig rulesLow hudData s history () =
    let stack = min (s.HeroStack + s.HeroBet) (s.VillainStack + s.VillainBet)
    let effectiveStack = decimal stack / decimal s.BB
    let callSize = min (s.VillainBet - s.HeroBet) s.HeroStack
    let potOdds = (callSize |> decimal) * 100m / (s.VillainBet + s.HeroBet + callSize |> decimal) |> ceil |> int
    let hudStats = hud hudData s.VillainName
    let openRaise = (if s.BB >= 20 then hudStats.OpenRaise20_25 else if s.BB >= 16 then hudStats.OpenRaise16_19 else hudStats.OpenRaise14_15) |> decimal
    if hudStats.LimpFold >= 65 then decideOnRules rulesBig effectiveStack potOdds openRaise history s.HeroHand
    else decideOnRules rulesLow effectiveStack potOdds openRaise history s.HeroHand     