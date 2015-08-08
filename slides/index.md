- title : パーザコンビネータをつくろう
- description : パーザンコビネータをつくる
- author : pocketberserker
- theme : sky
- transition : default

***

## パーザコンビネータをつくろう

PEGと構文解析に関するアレコレの勉強会 Vol.1

***

### 自己紹介

![icon](https://camo.githubusercontent.com/5dbd18d5fc15054677aaab64d647c4a076483af4/68747470733a2f2f646c2e64726f70626f7875736572636f6e74656e742e636f6d2f752f35373437383735382f7062736b2e6a7067)

* なかやん・ゆーき / ぺんぎん / もみあげ
* [@pocketberserker](https://twitter.com/pocketberserker)
* Microsoft MVP for <del>F#</del> .NET (2015/04/01～ 2016/03/31)
* パーザコンビネータライブラリはてきとーにwatchしてる

***

### パーザコンビネータ is 何

[Wikipedia](https://en.wikipedia.org/wiki/Parser_combinator) 曰く

> In functional programming, a parser combinator is a higher-order function that accepts several parsers as input and returns a new parser as its output.

てきとー訳

> 関数プログラミングでは、パーザコンビネータは入力としていくつかのパーザを受け取り、出力として新しいパーザを返す高階関数です。

なるほどね

***

### パーザコンビネータライブラリ

* [Parsec](https://github.com/aslatter/parsec)
* [Trifecta](https://github.com/ekmett/trifecta)
* [scala-parser-combinators](https://github.com/scala/scala-parser-combinators)
* [nom](https://github.com/Geal/nom)
* Boost Spirit
* [parsimmon](https://github.com/jneen/parsimmon)

他にもたくさん

***

### 私とパーザコンビネータ

* [FsAttoparsec](https://github.com/pocketberserker/FsAttoparsec) 作ってみたり
* [SmlSharpContrib](https://github.com/bleis-tift/SmlSharpContrib) にそれっぽいもの作ってみたり
* [atto](https://github.com/tpolecat/atto) に手を入れたり
* 仕事で Boost.Spirit 使ってみたり

***

### ところで

会場に

* パーザコンビネータ使ったことがある方
* パーザコンビネータ実装したことがある方

が多い場合、以降の発表内容を考える?

***

### パーザコンビネータの学び方

[FP in Scala](http://book.impress.co.jp/books/1114101091) の第9章

![tweet](./images/fp-in-scala.png)

***

### どゆこと?

* 実装方法を知れば使い方もわかるよね、的な
* というわけで、簡単なものを作ってみましょう
* **F#** で

***

### 解析を実行する関数の型を考える

パーザと入力を受け取って解析結果を返せばよい…気がする

    val run: Parser<'T> -> string -> Result<'T>

* Parser: パーザ
* Result: 解析結果
* `'T`は解析成功時に得られるデータ

それっぽい

***

### Parser<'T> について考える

* 解析開始位置があれば解析できそう
* 何か操作をすれば `Result<'T>` を返そう

***

### Parser<'T> の実態

    // なんかレコード
    type State = {
      Input: string
      Pos: int
    }
    // 今回は単なる関数のtype alias
    type Parser<'T> = State -> Result<'T>

実装次第では `pos` だけでいい

***

### Result<'T> その1

    // Haskell だと Maybe
    type Result<'T> = 'T option

* 要件は満たす
* が、解析失敗時の情報が全くない
* もうちょっと情報を増やそう

***

### Result<'T> その2

    // Haskell だと Either
    type Result<'T> = Choice<'T, string>

* 成功時は `'T`、 失敗時は `string`
* よさげ
* でも名前がわかりにくい
* ちょっと定義し直し

***

### Result<'T> その3

    type Result<'T> =
    | Success of 'T
    | Failure of string

* `string` ではなく `NonEmptyList[String]` とかのほうがいいけど今回は略
* なんか足りない気がする

***

### Result<'T> その4

解析成功時のpositionがあったほうが合成しやすそう

    type Result<'T> =
    | Success of 'T * int
    | Failure of string

`'T * State` のほうがよさそうだけど今回は略

***

### run 関数実装

    let run (parser: Parser<'T> input =
      let init = { Input = input; Pos = 0 }
      parser init

***

### 実際にパーザを作る

指針がほしいので、以下の二つを足がかりにする

* PEG
* Parser Monad

以降のスライドでは一部実装を省略します

***

### 文字列リテラル(PEG)

    let pstring str = fun s ->
      match State.extract s with
      // 入力の先頭に一致したら成功
      | Some target when target.StartsWith(str) -> Success(str, s.Pos + String.length str)
      | Some _ -> Failure (sprintf "文字列\"%s\"にマッチしませんでした" str)
      | None -> Failure "pstring: もう入力がないよ"

***

### ワイルドカード(PEG)

    // 位置文字あればよい
    let any = fun s ->
      match State.extract s with
      | Some target when not <| String.IsNullOrEmpty(target) -> Success(target.Chars(0), s.Pos + 1)
      | _ -> Failure "any: もう入力がないよ"

***

### 連接(PEG)

    let (<.>) a b = fun s ->
      a s
      |> Result.bind (fun r1 rpos1 ->
        // 一つ目の解析に成功したらpositionを更新し
        State.update rpos1 s
        |> b
        // 二つめの解析を試みる
        |> Result.map (fun r2 rpos2 -> ((r1, r2), rpos2)))

両方に成功しないと成功とみなさない

***

### 選択(PEG)

a に失敗したら b で解析を試みる

    let (</>) a b = fun s ->
      match a s with
      | Success _ as p -> p
      | _ -> b s

***

### 繰り返し(PEG)

    let many p =
      let rec inner acc s =
        match p s with
        | Success(v, rpos1) -> inner (v :: acc) (State.update rpos1 s)
        | Failure _ -> Success(List.rev acc, s.Pos)
      fun s -> inner [] s

* 失敗するまでひたすらpositionを更新しながら解析
* `many1` は `andThen` と List.cons を使えばよい

***

### 否定先読み(PEG)

    let bang p = fun s ->
      match p s with
      | Success _ -> Failure "bang"
      | Failure _ -> Success((), s.Pos)

***

### ok関数(Monad, return の代わり)

    let ok value = fun s -> Success(value, s.Pos)

パーザ内で条件分岐させた後にパーザを返したりするときに便利

***

### bind関数(Monad)

    let bind f p = fun s ->
      match p s with
      | Success(value, pos) -> s |> State.update pos |> f value
      | Failure r -> Failure r

***

### map関数(Functor)

    let map f (p: Parser<_>) = fun s ->
      match p s with
      | Success(value, pos) -> Success(f value, pos)
      | Failure r -> Failure r

解析結果を変換したいよね？

***

### まとめ

* パーザコンビネータの仕組み自体は簡単、こわくない
* 実際はパフォーマンスチューニングのためにもっと色々やってるけど
* 実際はエラー出力のために(ry
* 構文解析楽しく学ぼう

