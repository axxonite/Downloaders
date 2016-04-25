module Utilities

open System.Collections.Generic

type EnumStringPair<'a> = 
    { enum : 'a
      string : string }

let matchEnum enum pairs = (pairs |> Seq.find (fun p -> p.enum.Equals(enum))).string

let DictOfSeq(s : ('k * 'v) seq) = new Dictionary<'k, 'v>(s |> Map.ofSeq) :> IDictionary<'k, 'v>