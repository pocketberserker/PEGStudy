namespace PEGStudy

open System

type State = {
  Input: string
  Pos: int
}

type Result<'T> =
  | Success of value: 'T * pos: int
  | Failure of reason: string

type Parser<'T> = State -> Result<'T>

[<RequireQualifiedAccess>]
module String =

  let extract i j s =
    if String.length s < i then None
    else
      match j with
      | Some j -> Some(s.Substring(i, j))
      | None -> Some(s.Substring(i))

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module State =

  let initialize input = { Input = input; Pos = 0 }

  let update pos s = { s with Pos = pos }

  let extract s = String.extract s.Pos None s.Input

module Result =

  let map f = function
  | Success(v, p) -> Success(f v p)
  | Failure r -> Failure r

  let bind f = function
  | Success(v, p) -> f v p
  | Failure r -> Failure r

module Parser =

  // Monad

  let ok value = fun s -> Success(value, s.Pos)

  let error reason = fun (_: State) -> Failure reason

  let map f (p: Parser<_>) = fun s ->
    match p s with
    | Success(value, pos) -> Success(f value, pos)
    | Failure r -> Failure r

  let bind f p = fun s ->
    match p s with
    | Success(value, pos) -> s |> State.update pos |> f value
    | Failure r -> Failure r

  // PEG

  let (<.>) a b = fun s ->
    a s
    |> Result.bind (fun r1 rpos1 ->
      State.update rpos1 s
      |> b
      |> Result.map (fun r2 rpos2 -> ((r1, r2), rpos2)))

  let opt p = fun s ->
    match p s with
    | Success(v, p) -> Success(Some v, p)
    | Failure _ -> Success(None, s.Pos)

  let many p =
    let rec inner acc s =
      match p s with
      | Success(v, rpos1) -> inner (v :: acc) (State.update rpos1 s)
      | Failure _ -> Success(List.rev acc, s.Pos)
    fun s -> inner [] s

  let many1 p = p <.> (many p) |> map (fun (a, b) -> a :: b)

  let bang p = fun s ->
    match p s with
    | Success _ -> Failure "bang"
    | Failure _ -> Success((), s.Pos)

  let amp p = bang (bang p)

  let (</>) a b = fun (s: State) ->
    match a s with
    | Success _ as p -> p
    | _ -> b s

  let choice xs = List.fold (</>) (error "パーサーがひとつも指定されたなかったよ") xs

  let sepBy1 p s = p <.> (many (bind (fun _ -> p) s)) |> map (fun (a, b) -> a :: b)
  let sepBy p s = sepBy1 p s </> ok []

  // char

  let any = fun s ->
    match State.extract s with
    | Some target when not <| String.IsNullOrEmpty(target) -> Success(target.Chars(0), s.Pos + 1)
    | _ -> Failure "any: もう入力がないよ"

  // string

  let pstring str = fun s ->
    match State.extract s with
    | Some target when target.StartsWith(str) -> Success(str, s.Pos + String.length str)
    | Some _ -> Failure (sprintf "文字列\"%s\"にマッチしませんでした" str)
    | None -> Failure "pstring: もう入力がないよ"

  // runner

  let run (parser: Parser<'T>) input =
    State.initialize input |> parser
