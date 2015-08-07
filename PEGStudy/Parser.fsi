namespace PEGStudy

type State = {
  Input: string
  Pos: int
}

type Result<'T> =
  | Success of value: 'T * pos: int
  | Failure of reason: string

type Parser<'T> = State -> Result<'T>

module Parser =

  val run: Parser<'T> -> string -> Result<'T>
  val ok: 'T -> Parser<'T>
  val error: string -> Parser<'T>
  val map: ('T -> 'U) -> Parser<'T> -> Parser<'U>
  val bind: ('T -> Parser<'U>) -> Parser<'T> -> Parser<'U>
  val andThen : Parser<'T> -> Parser<'U> -> Parser<'T * 'U>
  val opt: Parser<'T> -> Parser<'T option>
  val many: Parser<'T> -> Parser<'T list>
  val many1: Parser<'T> -> Parser<'T list>
  val bang: Parser<'T> -> Parser<unit>
  val amp: Parser<'T> -> Parser<unit>
  val orElse: Lazy<Parser<'T>> -> Parser<'T> -> Parser<'T>
  val choice: Parser<'T> list -> Parser<'T>
  val sepBy1: Parser<'T> -> Parser<'U> -> Parser<'T list>
  val sepBy: Parser<'T> -> Parser<'U> -> Parser<'T list>
  val any: Parser<char>
  val pstring: string -> Parser<string>
