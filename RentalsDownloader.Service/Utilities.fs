[<AutoOpen>]
module Utilities

open System.Collections.Generic

type EnumStringPair<'a> = 
    { enum : 'a
      string : string }

let matchEnum enum pairs = (pairs |> Seq.find (fun p -> p.enum.Equals(enum))).string
let DictOfSeq(s : ('k * 'v) seq) = new Dictionary<'k, 'v>(s |> Map.ofSeq) :> IDictionary<'k, 'v>

let toDict m s = 
    s
    |> Seq.map (fun x -> ((m x), x))
    |> DictOfSeq

let boolToInvInt b = 
    if b then 0
    else 1

let action seq lambda startIndex = 
    seq
    |> List.fold (fun i x -> (lambda i x) i + 1) startIndex
    |> ignore

let assignIndicesFunc f list = fst (list |> List.fold (fun (result, i) x -> ((f i, x) :: result, i + 1)) (List.empty, 0))
let assignIndices list = assignIndicesFunc (fun i -> i) list
