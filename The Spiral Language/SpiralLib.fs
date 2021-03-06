﻿module Spiral.Lib
open CoreLib
open Main

let option =
    (
    "Option",[],"The Option module.",
    """
inl Option x = .Some, x \/ .None

inl some x = box (Option x) (.Some, x)
inl none x = box (Option x) (.None)

{Option some none} 
|> stackify
    """) |> module_

let lazy_ =
    (
    "Lazy",[option],"The Lazy module.",
    """
inl lazy f =
    met f x = f x
    inl ty = type (f ())
    inl x = Option.none ty |> ref
    function
    | .value -> join (
        match x() with
        | .None ->
            inl r = f()
            x := Option.some r
            r
        | .Some, r -> r
        )
    | .elem_type -> ty

{lazy} 
|> stackify
    """) |> module_

let tuple =
    (
    "Tuple",[],"Operations on tuples.",
    """
inl singleton x = x :: ()
inl head x :: xs = x
inl tail x :: xs = xs
inl rec last x :: xs = match xs with () -> x | xs -> last xs

inl wrap = function
    | _ :: _ | () as x -> x
    | x -> x :: ()

inl unwrap (x :: () | x) = x

inl rec foldl f s = function
    | x :: xs -> foldl f (f s x) xs
    | () -> s

inl rec foldr f l s = 
    match l with
    | x :: xs -> f x (foldr f xs s)
    | () -> s

inl reducel f l =
    match l with
    | x :: xs -> foldl f x xs
    | () -> error_type "Reduce must receive a non-empty tuple as input."

inl rec scanl f s = function
    | x :: xs -> s :: scanl f (f s x) xs
    | () -> s :: ()

inl rec scanr f l s = 
    match l with
    | x :: xs -> 
        inl r = scanr f xs s
        f x (head r) :: r
    | () -> s :: ()

inl append = foldr (::)
inl concat x = foldr append x ()

inl rev, map =
    inl map' f l = foldl (inl s x -> f x :: s) () l
    inl rev l = map' id l
    inl map f = map' f >> rev
    rev, map

inl iter f = foldl (const f) ()
inl iteri f = foldl f 0

inl rec choose f = function
    | a :: a' ->
        match f a with
        | () -> choose f a'
        | x -> x :: choose f a'
    | () -> ()

inl rec map2 f a b = 
    match a,b with
    | a :: as', b :: bs' -> f a b :: map2 f as' bs'
    | (), () -> ()
    | _ -> error_type "The two tuples have uneven lengths." 

inl rec choose2 f a b = 
    match a, b with
    | a :: as', b :: bs' -> 
        match f a b with
        | () -> choose2 f as' bs'
        | x -> x :: choose2 f as' bs'
    | (), () -> ()
    | _ -> error_type "The two tuples have uneven lengths." 

inl rec iter2 f a b = 
    match a,b with
    | a :: as', b :: bs' -> f a b; iter2 f as' bs'
    | (), () -> ()
    | _ -> error_type "The two tuples have uneven lengths." 

inl rec map3 f a b c = 
    match a,b,c with
    | a :: as', b :: bs', c :: cs' -> f a b c :: map3 f as' bs' cs'
    | (), (), () -> ()
    | _ -> error_type "The three tuples have uneven lengths." 

inl rec foldl2 f s a b =
    match a,b with
    | a :: as', b :: bs' -> foldl2 f (f s a b) as' bs'
    | (), () -> s
    | _ -> error_type "The two tuples have uneven lengths." 

inl rec foldr2 f a b s = 
    match a,b with
    | a :: a', b :: b' -> f a b (foldr2 f a' b' s)
    | (), () -> s

inl rec forall f = function
    | x :: xs -> f x && forall f xs
    | () -> true

inl rec exists f = function
    | x :: xs -> f x || exists f xs
    | () -> false

inl rec filter f = function
    | x :: xs -> if f x then x :: filter f xs else filter f xs
    | () -> ()

inl is_empty = function
    | _ :: _ -> false
    | () -> true
    | _ -> error_type "Not a tuple."

inl is_non_empty_tuple = function
    | _ :: _ -> true
    | _ -> false

inl transpose l on_fail on_succ =
    inl rec loop acc_total acc_head acc_tail l = 
        match l with
        | () :: ys ->
            match acc_head with
            | () when forall is_empty ys ->
                match acc_total with
                | _ :: _ -> rev acc_total |> on_succ
                | () -> error_type "Empty inputs in the inner dimension to transpose are invalid."
            | _ -> on_fail()
        | (x :: xs) :: ys -> loop acc_total (x :: acc_head) (xs :: acc_tail) ys
        | _ :: _ -> on_fail ()
        | () -> 
            match acc_tail with
            | _ :: _ -> loop (rev acc_head :: acc_total) () () (rev acc_tail)
            | () -> rev acc_total |> on_succ
    loop () () () l

inl zip_template on_ireg l = 
    inl rec zip = function // when forall is_non_empty_tuple l 
        | _ :: _ as l -> transpose l (inl _ -> on_ireg l) (map (function | x :: () -> zip x | x -> x))
        | () -> error_type "Zip called on an empty tuple."
        | _ -> error_type "Zip called on a non-tuple."
    zip l

inl regularity_guard l =
    if forall is_empty l then l
    else error_type "Irregular inputs in unzip/zip."
inl zip = zip_template regularity_guard
inl zip' = zip_template id

inl rec unzip_template on_irreg l = 
    inl rec unzip = function
        | _ :: _ as l when forall is_non_empty_tuple l -> transpose (map unzip l) (inl _ -> on_irreg l) id 
        | _ :: _ -> l
        | () -> error_type "Unzip called on an empty tuple."
        | _ -> error_type "Unzip called on a non-tuple."
    unzip l

inl unzip = unzip_template regularity_guard
inl unzip' = unzip_template id

inl init_template k n f =
    inl rec loop n = 
        match n with 
        | n when n > 0 -> 
            inl n = n - 1
            f n :: loop n
        | 0 -> ()
        | _ -> error_type "The input to this function cannot be static or less than 0 or not an int."
    loop n |> k

inl init = init_template rev
inl repeat n x = init_template id n (inl _ -> x)
inl range (min,max) = 
    inl l = max-min+1
    if l > 0 then init l ((+) min)
    else error_type "The inputs to range must be both static and the length of the resulting tuple must be greater than 0."

inl rec tryFind f = function
    | x :: xs -> if f x then .Some, x else tryFind f xs
    | () -> .None

inl rec contains t x = 
    match tryFind ((=) x) t with
    | .Some, x -> true
    | .None -> false

inl rec intersperse sep = function
    | _ :: () as x -> x
    | x :: xs -> x :: sep :: intersperse sep xs
    | _ -> error_type "Not a tuple."

inl split_at n l =
    assert (lit_is n) "The index must be a literal."
    assert (n >= 0) "The input must be positive or zero."
    inl rec loop n a b = 
        if n > 0 then 
            match b with
            | x :: x' -> loop (n-1) (x :: a) x'
            | _ -> error_type "Index out of bounds."
        else
            (rev a, b)
    loop n () l

inl take n l = split_at n l |> fst
inl drop n l = split_at n l |> snd

inl rec foldl_map f s l = 
    match l with
    | l :: l' ->
        inl l, s = f s l
        inl l', s = foldl_map f s l'
        l :: l', s
    | () -> (), s

inl rec foldr_map f l s = 
    match l with
    | l :: l' ->
        inl l', s = foldr_map f l' s
        inl l, s = f l s
        l :: l', s
    | () -> (), s

inl mapi f = foldl_map (inl s x -> f s x, s + 1) 0 >> fst

inl rec find f = function
    | x :: () -> if f x then x else failwith x "Key cannot be found."
    | x :: x' -> if f x then x else find f x'
    | _ -> error_type "Expected a non-empty tuple as input to this."

inl rec foldl_map2 f s a b = 
    match a,b with
    | a :: a', b :: b' ->
        inl l, s = f s a b
        inl l', s = foldl_map2 f s a' b'
        l :: l', s
    | (), () -> (), s

inl rec map_last f = function
    | x :: () -> f x :: ()
    | x :: x' -> x :: map_last f x'
    | () -> error_type "Must not be an empty tuple."
    | _ -> error_type "Must be a tuple."

inl length = foldl (inl s _ -> s+1) 0

{
head tail last foldl foldr reducel scanl scanr rev map iter iteri iter2 forall exists split_at take drop
filter zip unzip init repeat append concat singleton range tryFind contains intersperse wrap unwrap
foldl_map foldl_map2 foldr_map map2 foldl2 foldr2 choose choose2 mapi find map_last map3 length
} 
|> stackify
    """) |> module_

let liple =
    (
    "Liple",[],"Operations on tuples and singletons.",
    """
inl rec map f a =
    match a with
    | a :: a' -> f a :: map f a'
    | () -> ()
    | a -> f a

inl rec choose f = function
    | a :: a' ->
        match f a with
        | () -> choose f a'
        | x -> x :: choose f a'
    | () -> ()
    | a -> f a

inl rec map2 f a b = 
    match a,b with
    | a :: as', b :: bs' -> f a b :: map2 f as' bs'
    | (), () -> ()
    | a, b -> f a b

inl rec choose2 f a b = 
    match a, b with
    | a :: as', b :: bs' -> 
        match f a b with
        | () -> choose2 f as' bs'
        | x -> x :: choose2 f as' bs'
    | (), () -> ()
    | a, b -> f a b

inl rec iter2 f a b = 
    match a,b with
    | a :: as', b :: bs' -> f a b; iter2 f as' bs'
    | (), () -> ()
    | a, b -> f a b; ()

inl rec map3 f a b c = 
    match a,b,c with
    | a :: as', b :: bs', c :: cs' -> f a b c :: map3 f as' bs' cs'
    | (), (), () -> ()
    | a, b, c -> f a b c

inl rec foldl f s = function
    | x :: xs -> foldl f (f s x) xs
    | () -> s
    | x -> f s x

inl rec foldr f l s = 
    match l with
    | x :: xs -> f x (foldr f xs s)
    | () -> s
    | x -> f x s

inl rec foldl2 f s a b =
    match a,b with
    | a :: as', b :: bs' -> foldl2 f (f s a b) as' bs'
    | (), () -> s
    | a, b -> f s a b

inl rec foldr2 f a b s = 
    match a,b with
    | a :: a', b :: b' -> f a b (foldr2 f a' b' s)
    | (), () -> s
    | a, b -> f a b s

inl rec foldl_map f s l = 
    match l with
    | l :: l' ->
        inl l, s = f s l
        inl l', s = foldl_map f s l'
        l :: l', s
    | () -> (), s
    | l ->
        inl l, s = f s l
        l, s

inl rec foldr_map f l s = 
    match l with
    | l :: l' ->
        inl l', s = foldr_map f l' s
        inl l, s = f l s
        l :: l', s
    | () -> (), s
    | l ->
        inl l, s = f l s
        l, s

inl mapi f = foldl_map (inl s x -> f s x, s + 1) 0 >> fst

{map choose map2 choose2 iter2 map3 foldl foldr foldl2 foldr2 foldl_map foldr_map mapi} |> stackify
    """
    ) |> module_

let loops =
    (
    "Loops",[tuple],"The Loop module.",
    """
inl rec unroll f x = if eq_type x (type f x) then x else unroll f (f x)

inl rec while {cond body state} as d =
    inl loop_body {state cond body} as d =
        if cond state then while {d with state=body state}
        else state
    match d with
    | {static} -> loop_body d
    | _ -> (met _ -> loop_body d : state) ()

inl for_template kind {d with body} =
    inl finally =
        match d with
        | {finally} -> finally
        | _ -> id

    inl state = 
        match d with
        | {state} -> state
        | _ -> ()

    inl check =
        match d with
        | {near_to} from -> from < near_to 
        | {to} from -> from <= to
        | {down_to} from -> from >= down_to
        | {near_down_to} from -> from > near_down_to
        | _ -> error_type "Only one of `to`,`near_to`,`down_to`,`near_down_to` needs be present."

    inl to =
        match d with
        | {(to ^ near_to ^ down_to ^ near_down_to)=to} -> to
        | _ -> error_type "Only one of `to`,`near_to`,`down_to`,`near_down_to` is allowed."

    inl by =
        match d with
        | {by} -> by
        | {to | near_to} -> 1
        | {down_to | near_down_to} -> -1

    inl rec loop {from state} =
        inl body {from} = 
            if check from then 
                match kind with
                | .CPSd ->
                    inl next state = loop {state from=from+by}
                    body {next state i=from}
                | _ ->
                    loop {state=body {state i=from}; from=from+by}

            else finally state

        if Tuple.forall lit_is (from,to,by) then body {from}
        else 
            inl from = dyn from
            join body {from} : finally state

    match kind with
    | .UnrolledState ->
        inl f {state from} = 
            assert (check from) "The loop must be longer than the number of steps in which the state's type is unconverged."
            {state=body {state i=from}; from=from+by}

        match d with
        | {from} -> unroll f {state from} |> inl {state from} -> loop {state from=dyn from}
        | _ -> error_type "Only `from` field is allowed in the state unrolling loop."
    | _ -> 
        match d with
        | {from=(!dyn from) ^ static_from=(!lit_is true) & from} -> loop {from state}
        | _ -> error_type "Only one of `from` and `static_from` field to loop needs to be present. In addition `static_from` needs to be a literal if present."
        

inl for' = for_template .CPSd
inl for = for_template .Standard
inl foru = for_template .UnrolledState

{for for' foru while unroll} 
|> stackify
    """) |> module_

let extern_ =
    (
    "Extern",[tuple;loops],"The Extern module.",
    """
inl dot = ."."
inl space = ." "
inl FS = {
    Constant = inl a t -> !MacroFs(t, [text: a])
    StaticField = inl a b t -> !MacroFs(t, [
        type: a
        text: dot
        text: b
        ])
    Field = inl a b t -> !MacroFs(t, [
        arg: a
        text: dot
        text: b
        ])
    Method = inl a b c t -> !MacroFs(t, [
        arg: a
        text: dot
        text: b
        args: c
        ])
    StaticMethod = inl a b c t -> !MacroFs(t, [
        type: a
        text: dot
        text: b
        args: c
        ])
    Constructor = inl a b -> !MacroFs(a, [
        type: a
        args: b
        ])
    UnOp = inl op a t -> !MacroFs(t,[
        text: op
        text: space
        arg: a
        ])
    BinOp = inl a op b t -> !MacroFs(t,[
        arg: a
        text: op
        arg: b
        ])
    }

inl closure_of_template check_range = 
    inl rec loop f tys =
        match tys with
        | x => xs -> term_cast (inl x -> loop (f x) xs) x
        | x when check_range && eq_type f x = false -> error_type "The tail of the closure does not correspond to the one being casted to."
        | _ -> f
    loop

inl closure_of' = closure_of_template false
inl closure_of = closure_of_template true

inl (use) a b =
    inl r = b a
    FS.Method a .Dispose() ()
    r

// Optimized to do more work at compile time. Will flatten nested tuples.
inl string_concat sep = 
    inl rec loop x = 
        Tuple.foldr (inl x {state with dyn stc} ->
            match x with
            | _ :: _ -> loop x state
            | x when lit_is x -> {state with stc=x::stc}
            | x -> 
                match stc with
                | _ :: _ -> {dyn=x :: string_concat sep stc :: dyn; stc=()}
                | _ -> {dyn=x :: dyn; stc=()}
            ) x
    function
    | _ :: _ as l ->
        inl {dyn stc} = loop l {dyn=(); stc=()}
        Tuple.append stc dyn |> string_concat sep
    | x -> x

inl rec show' cfg =
    inl show x = show' cfg x
    met show_array !dyn (array_cutoff, ar) = 
        inl strb_type = fs [text: "System.Text.StringBuilder"]
        inl s = FS.Constructor strb_type ()
        inl append x = FS.Method s .Append x strb_type |> ignore

        append "[|"
        Loops.for {from=0; near_to=min (array_length ar) array_cutoff; state=dyn ""; body=inl {state=prefix i} ->
            append prefix; append (show (ar i)); dyn "; "
            } |> ignore
        append "|]"
        FS.Method s .ToString() string
    inl show_tuple l = 
        Tuple.foldr (inl v s -> show v :: s) l ()
        |> string_concat ", "
        |> string_format "[{0}]"
    inl show_module m = 
        inl x = module_foldr (inl .(k) v s -> string_format "{0} = {1}" (k, show v) :: s) m () 
        string_format "{0}{1}{2}" ("{", string_concat "; " x, "}")
    
    function
    | {} as m -> show_module m
    | () -> "[]"
    | _ :: _ as l -> show_tuple l
    | (@array_is x) as ar ->
        match x with
        | .DotNetHeap -> 
            inl array_cutoff = match cfg with {array_cutoff} -> array_cutoff | _ -> FS.Constant "System.Int64.MaxValue" int64
            show_array (array_cutoff, ar)
        | .DotNetReference -> show (ar ())
    | x -> cfg.show_value x

inl show_value = function 
    | .(x) & x : string | x : string -> x
    | x -> string_format "{0}" x
inl show = show' {array_cutoff = 30; show_value}

inl assert c msg =
    if c = false then
        if lit_is c then error_type msg
        else failwith () (show msg)

{string_concat closure_of closure_of' FS (use) show' show assert } 
|> stackify
    """) |> module_


let array =
    (
    "Array",[tuple;loops],"The array module",
    """
open Loops

/// Creates an empty array with the given type.
/// t -> t array
inl empty t = array_create t 0

/// Creates a singleton array with the given element.
/// x -> t array
inl singleton x =
    inl ar = array_create x 1
    ar 0 <- x
    ar

/// Applies a function to each elements of the collection, threading an accumulator argument through the computation.
/// If the input function is f and the elements are i0..iN then computes f..(f i0 s)..iN.
/// (s -> a -> s) -> s -> a array -> s
inl foldl f state ar = for {from=0; near_to=array_length ar; state; body=inl {state i} -> f state (ar i)}

/// Applies a function to each element of the array, threading an accumulator argument through the computation. 
/// If the input function is f and the elements are i0...iN then computes f i0 (...(f iN s)).
/// (a -> s -> a) -> a array -> s -> s
inl foldr f ar state = for {from=array_length ar-1; down_to=0; state; body=inl {state i} -> f (ar i) state}

// Creates an array given a dimension and a generator function to compute the elements.
// ?(.is_static) -> int -> (int -> a) -> a array
inl init = 
    inl body is_static n f =
        assert (n >= 0) "The input to init needs to be greater or equal to 0."
        inl typ = type (f (dyn 0))
        inl ar = array_create typ n
        inl d = 
            inl d = {near_to=n; body=inl {i} -> ar i <- f i}
            if is_static then {d with static_from = 0} else {d with from = 0}
        for d
        ar
    function
    | .static -> body true
    | n -> body false n

/// Builds a new array that contains elements of a given array.
/// a array -> a array
met copy ar = init (array_length ar) ar

/// Builds a new array whose elements are the result of applying a given function to each of the elements of the array.
/// (a -> b) -> a array -> a array
inl map f ar = init (array_length ar) (ar >> f)

/// Returns a new array containing only elements of the array for which the predicate function returns `true`.
/// (a -> bool) -> a array -> a array
inl filter f ar =
    inl ar' = array_create ar.elem_type (array_length ar)
    inl count = foldl (inl s x -> if f x then ar' s <- x; s+1 else s) (dyn 0) ar
    init count ar'

/// Merges all the arrays in a tuple into a single one.
/// a array tuple -> a array
inl append l =
    inl ar' = array_create (fst l).elem_type (Tuple.foldl (inl s l -> s + array_length l) 0 l)
    inl ap s ar = foldl (inl i x -> ar' i <- x; i+1) s ar
    Tuple.foldl ap (dyn 0) l |> ignore
    ar'

/// Flattens an array of arrays into a single one.
/// a array array -> a array
inl concat ar =
    inl count = foldl (inl s ar -> s + array_length ar) (dyn 0) ar
    inl ar' = array_create ar.elem_type.elem_type count
    (foldl << foldl) (inl i x -> ar' i <- x; i+1) (dyn 0) ar |> ignore
    ar'

/// Tests if all the elements of the array satisfy the given predicate.
/// (a -> bool) -> a array -> bool
inl forall f ar = for' {from=0; near_to=array_length ar; state=true; body = inl {next state i} -> f (ar i) && next state}

/// Tests if any the element of the array satisfies the given predicate.
/// (a -> bool) -> a array -> bool
inl exists f ar = for' {from=0; near_to=array_length ar; state=false; body = inl {next state i} -> f (ar i) || next state}

inl sort ar = macro.fs ar [text: "Array.sort "; arg: ar]
inl sort_descending ar = macro.fs ar [text: "Array.sortDescending "; arg: ar]

/// Applies the function to every element of the array.
/// (a -> unit) -> a array -> unit
inl iter f x = for {from=0; near_to=array_length x; body=inl {i} -> f (x i)}

/// Shuffles the array inplace. Takes in a range.
/// a array -> {from near_to} -> unit
met knuth_shuffle rnd {from near_to} ar =
    inl swap i j =
        inl item = ar i
        ar i <- ar j
        ar j <- item

    Loops.for {from near_to=near_to-1; body=inl {i} -> swap i (rnd.next(to int32 i, to int32 near_to))}

/// Shuffles the array inplace.
/// a array -> unit
inl shuffle_inplace rnd x = knuth_shuffle rnd {from=0; near_to=array_length x} x
    
/// Shuffles the array.
/// a array -> a array
inl shuffle rnd x =
    inl x' = copy x
    shuffle_inplace rnd x'
    x'

{empty singleton foldl foldr init copy map filter append concat forall exists sort sort_descending iter knuth_shuffle shuffle_inplace shuffle}
|> stackify
    """) |> module_

let list =
    (
    "List",[loops;option;tuple],"The List module.",
    """
inl rec List x = join_type () \/ x, List x

/// Creates an empty list with the given type.
/// t -> t List
inl empty x = box (List x) ()

/// Creates a single element list with the given type.
/// x -> x List
inl singleton x = box (List x) (x, empty x)

/// Immutable appends an element to the head of the list.
/// x -> x List -> x List
inl cons a b = 
    inl t = List a
    box t (a, box t b)

/// Creates a list by calling the given generator on each index.
/// ?(.static) -> int -> (int -> a) -> a List
inl init =
    inl body is_static n f =
        inl t = type (f 0)
        inl d = {near_to=n; state=empty t; body=inl {next i state} -> cons (f i) (next state)}
        if is_static then Loops.for' {d with static_from=0}
        else Loops.for' {d with from=0}

    function
    | .static -> body true
    | x -> body false x

/// Returns the element type of the list.
/// a List -> a type
inl elem_type l =
    match split l with
    | (), (a,b) when eq_type (List a) l -> a
    | _ -> error_type "Expected a List in elem_type."

/// Builds a new list whose elements are the results of applying the given function to each of the elements of the list.
/// (a List -> b List) -> a List -> b List
inl rec map f l = 
    inl t = elem_type l
    inl loop = function
        | x,xs -> cons (f x) (map f xs)
        | () -> empty t
    if box_is l then loop l
    else join loop l : List t

/// The CPS'd version of foldl.
inl rec foldl' finally f s l =
    inl loop = function
        | x, xs -> f (inl s -> foldl' finally f s xs) s x
        | () -> finally s
    if box_is l then loop l
    else join loop l : finally s

/// Applies a function f to each element of the collection, threading an accumulator argument through the computation. 
/// The fold function takes the second argument, and applies the function f to it and the first element of the list. 
/// Then, it feeds this result into the function f along with the second element, and so on. It returns the final result. 
/// If the input function is f and the elements are i0...iN, then this function computes f (... (f s i0) i1 ...) iN.
/// (s -> a -> s) -> s -> a List -> s
inl foldl f = foldl' id (inl next a b -> next (f a b))

/// Applies a function to each element of the collection, threading an accumulator argument through the computation. 
/// If the input function is f and the elements are i0...iN, then this function computes f i0 (...(f iN s)).
/// (a -> s -> s) -> a List -> s -> s
inl rec foldr f l s = 
    inl loop = function
        | x, xs -> f x (foldr f xs s)
        | () -> s
    if box_is l then loop l
    else join loop l : s

open Option

/// Returns the first element of the list.
/// a List -> {some=(a -> a) none=(a type -> a)} -> a
inl head' l {some none} =
    inl t = elem_type l
    match l with
    | x, xs -> some x
    | () -> none t

/// Returns the list without the first element.
/// a List -> {some=(a List -> a List) none=(a List type -> a List)} -> a List
inl tail' l {some none} =
    inl t = elem_type l
    match l with
    | x, xs -> some xs
    | () -> none (List t)

/// Returns the last element of the list.
/// a List -> {some=(a -> a) none=(a type -> a)} -> a
inl rec last' l {some none} =
    inl t = elem_type l
    inl loop = function
        | x, xs -> last' xs {some none=some x}
        | () -> none t
    if box_is l then loop l
    else join loop l : none t

/// Returns the first element of the list.
/// a List -> a Option
inl head l = head' l {some none}

/// Returns the list without the first element.
/// a List -> a List Option
inl tail l = tail' l {some none}

/// Returns the last element of the list.
/// a List -> a Option
inl last l = last' l {some=const << some; none}

/// Returns a new list that contains the elements of the first list followed by elements of the second.
/// a List -> a List -> a List
inl append = foldr cons

/// Returns a new list that contains the elements of each list in order.
/// a List List -> a List
inl concat l & !elem_type !elem_type t = foldr append l (empty t)

{List empty cons init map foldl' foldl foldr singleton head' tail' last' head tail last append concat} 
|> stackify
    """) |> module_

let parsing =
    (
    "Parsing",[extern_],"Parser combinators.",
    """
open Extern
// Primitives
inl m x = { 
    elem =
        match x with
        | {parser_rec} {d with on_type} state -> join (parser_rec d .elem d state : on_type)
        | {parser} -> parser
        | {parser_mon} -> parser_mon .elem
    }
inl term_cast p typ = m {
    parser = inl d state ->
        p .elem {d with 
            on_succ = 
                inl k = term_cast (inl x,state -> self x state) (typ,state)
                inl x state -> k (x,state)
            } state
    }
inl goto point x = m {
    parser = inl _ -> point x
    }
inl succ x = m {
    parser = inl {on_succ} -> on_succ x
    }
inl fail x = m {
    parser = inl {on_fail} -> on_fail x
    }
inl fatal_fail x = m {
    parser = inl {on_fatal_fail} -> on_fatal_fail x
    }
inl type_ = m {
    parser = inl {on_type on_succ} -> on_succ on_type
    }
inl state = m {
    parser = inl {on_succ} state -> on_succ state state
    }
inl set_state state = m {
    parser = inl {on_succ} _ -> on_succ () state
    }
inl (>>=) a b = m {
    parser = inl d -> a .elem {d with on_succ = inl x -> b x .elem d}
    }
inl try_with handle handler = m {
    parser = inl d -> handle .elem {d with on_fail = inl _ -> handler .elem d}
    }
inl guard cond handler = m {
    parser = inl {d with on_succ} state -> 
        if cond then on_succ () state 
        else handler .elem d state
    }
inl ifm cond tr fl = m {
    parser = inl d state -> if cond then tr () .elem d state else fl () .elem d state
    }
inl attempt a = m {
    parser = inl d state -> a .elem { d with on_fail = inl x _ -> self x state } state
    }

inl rec tuple = function
    | () -> succ ()
    | x :: xs ->
        inm x = x
        inm xs = tuple xs
        succ (x :: xs)

inl (|>>) a f = a >>= inl x -> succ (f x)
inl (.>>.) a b = tuple (a,b)
inl (.>>) a b = tuple (a,b) |>> fst
inl (>>.) a b = a >>= inl _ -> b // The way bind is used here in on purpose. `spaces` diverges otherwise due to loop not being evaled in tail position.
inl (>>%) a b = a |>> inl _ -> b

// TODO: Instead of just passing the old state on failure to the next parser, the parser should
// compare states and fail if the state changed. Right now that cannot be done because Spiral is missing
// polymorphic structural equality on all but primitive types. I want to be able to structurally compare anything.

// Though to be fair, in all the times I've used `choice`, I don't think there has been a single time it was without `attempt`.
// Unlike with Fparsec, backing up the state in Spiral is essentially a no-op due to inlining.
inl (<|>) a b = try_with (attempt a) b
inl choice = function
    | x :: xs -> Tuple.foldl (<|>) x xs
    | () -> error_type "choice require at lease one parser as input"

// CharParsers
inl convert_type = fs [text: "System.Convert"]
inl to_int64 x = FS.StaticMethod convert_type .ToInt64 x int64

inl is_digit x = x >= '0' && x <= '9'
inl is_whitespace x = x = ' '
inl is_newline x = x = '\n' || x = '\r'

inl string_stream str {idx on_succ on_fail} =
    inl f idx = idx >= 0 && idx < string_length str
    inl branch cond = if cond then on_succ (str idx) else on_fail "string index out of bounds" 
    match idx with
    | a, b -> branch (f a && f b)
    | _ -> branch (f idx)

inl stream_char = m {
    parser = inl {stream on_succ on_fail} {state with pos} ->
        stream {
            idx = pos
            on_succ = inl c -> on_succ c {state with pos=pos+1}
            on_fail = inl msg -> on_fail msg state
            }
    }

inl run data parser ret = 
    match data with
    | _ : string -> parser .elem { ret with stream = string_stream data} { pos = if lit_is data then 0 else dyn 0 }
    | _ -> error_type "Only strings supported for now."

inl with_unit_ret = {
    on_type = ()
    on_succ = inl _ _ -> ()
    on_fail = inl x _ -> failwith () x
    on_fatal_fail = inl x _ -> failwith () x
    }

inl run_with_unit_ret data parser = run data parser with_unit_ret

inl stream_char_pos =
    inm {pos} = state
    stream_char |>> inl x -> x,pos

inl satisfyL f m =
    inm s = state
    inm c = stream_char
    inm _ = guard (f c) (set_state s >>. fail m)
    succ c

inl (<?>) a m = try_with a (fail m)
inl pdigit = satisfyL is_digit "digit"
inl pchar c = satisfyL ((=) c) "char"

inl pstring (!dyn str) x = 
    inl rec loop (!dyn i) = m {
        parser_rec = inl {d with on_succ} ->
            ifm (i < string_length str)
            <| inl _ -> pchar (str i) >>. loop (i+1)
            <| inl _ -> succ str
        }
    loop 0 x

inl pint64 =
    inl rec loop handler i = m {
        parser_rec = inl {on_succ} ->
            inm c = try_with pdigit handler
            inl x = to_int64 c - to_int64 '0'
            inl max = 922337203685477580 // max int64 divided by 10
            inm _ = guard (i = max && x <= 7 || i < max) (fail "integer overflow")
            inl i = i * 10 + x
            loop (goto on_succ i) i
        }
    loop (fail "pint64") 0

/// Note: Unlike the Fparsec version, this spaces returns the number of spaces skipped.
inl spaces x =
    inl rec loop (!dyn i) = m {
        parser_rec = inl {on_succ} -> try_with (satisfyL (inl c -> is_whitespace c || is_newline c) "space") (goto on_succ i) >>. loop (i+1)
        }
    loop 0 x

inl parse_int =
    inm !dyn m = try_with (pchar '-' >>. succ false) (succ true)
    (pint64 |>> inl x -> if m then x else -x) .>> spaces

inl repeat n parser =
    inl rec loop (!dyn i) = m {
        parser_rec = inl _ ->
            ifm (i < n)
            <| inl _ -> parser i >>. loop (i+1)
            <| inl _ -> succ ()
        }
    loop 0

inl parse_array {parser typ n} = m {
    parser_mon =
        inm _ = guard (n >= 0) (fatal_fail "n in parse array must be >= 0")
        inl ar = array_create typ n
        repeat n (inl i -> parser |>> inl x -> ar i <- x) >>. succ ar
    }

// This function takes too long to compile so its use is not recommended.
// `show`, `string_concat` and `string_format` are better candidates as printing functions.
inl sprintf_parser append =
    inl rec sprintf_parser sprintf_state =
        inl parse_variable = m {
            parser_mon =
                inm c = try_with stream_char (inl x -> append '%'; fail "done" x)
                match c with
                | 's' -> function
                    | x : string -> x
                    | _ -> error_type "Expected a string in sprintf."
                | 'c' -> function
                    | x : char -> x
                    | _ -> error_type "Expected a char in sprintf."
                | 'b' -> function
                    | x : bool -> x
                    | _ -> error_type "Expected a bool in sprintf."
                | 'i' -> function
                    | x : int32 | x : int64 | x : uint32 | x : uint64 -> x
                    | _ -> error_type "Expected an integer in sprintf."
                | 'f' -> function
                    | x : float32 | x : float64 -> x
                    | _ -> error_type "Expected a float in sprintf."
                | 'A' -> id
                | _ -> error_type "Unexpected literal in sprintf."
                |> inl guard_type -> 
                    m { parser = inl d state -> d.on_succ (inl x -> append x; sprintf_parser .None .elem d state) state }
            }

        inl append_state = m {
            parser = inl {d with stream on_succ on_fail} state ->
                match sprintf_state with
                | .None -> on_succ () state
                | ab -> stream {
                    idx = ab
                    on_succ = inl r -> append r; on_succ () state 
                    on_fail = inl msg -> on_fail msg state
                    }
            }

        inm c = try_with stream_char_pos (append_state >>. fail "done")
        match c with
        | '%', _ -> append_state >>. parse_variable
        | _, pos ->
            match sprintf_state with
            | .None -> (pos, pos)
            | (start,_) -> (start, pos)
            |> sprintf_parser
    sprintf_parser .None

inl sprintf_template append ret format =
    run format (sprintf_parser append) ret

inl sprintf format = 
    inl strb_type = fs [text: "System.Text.StringBuilder"]
    inl strb = FS.Constructor strb_type (64i32)
    inl append x = FS.Method strb .Append x strb_type |> ignore
    sprintf_template append {
        on_succ = inl x _ -> x
        on_fail = inl msg _ -> FS.Method strb .ToString() string
        } format

{
run run_with_unit_ret succ fail fatal_fail state type_ tuple (>>=) (|>>) (.>>.) (.>>) (>>.) (>>%) (<|>) choice stream_char 
ifm (<?>) pdigit pchar pstring pint64 spaces parse_int repeat parse_array sprintf sprintf_template term_cast
} 
|> stackify
    """) |> module_


let console =
    (
    "Console",[extern_],"IO printing functions.",
    """
open Extern
inl console_type = fs [text: "System.Console"]
inl stream_type = fs [text: "System.IO.Stream"]
inl streamreader_type = fs [text: "System.IO.StreamReader"]
inl readall () = 
    FS.StaticMethod console_type .OpenStandardInput() stream_type
    |> FS.Constructor streamreader_type 
    |> inl x -> FS.Method x .ReadToEnd() string
inl readline () = FS.StaticMethod console_type .ReadLine() string

inl write = function
    | () -> ()
    | x -> FS.StaticMethod console_type .Write (show x) ()
inl writeline = function
    | () -> FS.StaticMethod console_type .WriteLine () ()
    | x -> FS.StaticMethod console_type .WriteLine (show x) ()

inl printf a b = string_format a b |> write
inl printfn a b = string_format a b |> writeline

{readall readline write writeline printf printfn}
|> stackify
    """) |> module_

let queue =
    (
    "Queue",[tuple;loops;console],"The queue module.",
    """
open Loops
open Console

inl add_one len x =
    inl x = x + 1
    if x = len then 0 else x

inl resize {len from to ar} =
    inl ar' = array_create (ar.elem_type) (len*3/2+3)
    for {from near_to=len; body=inl {i} -> ar' (i - from) <- ar i}
    for {from=0; near_to=from; body=inl {i} -> ar' (len - from + i) <- ar i}
    {from=0; to=len; ar=ar'}

met enqueue queue (!dyn v) =
    inl {from to ar} = queue
    ar to <- v
    inl len = array_length ar
    inl to = add_one len to
    if from = to then 
        inl {from to ar} = resize {len from to ar}
        queue.from <- dyn from
        queue.to <- dyn to
        queue.ar <- ar
    else queue.to <- dyn to

met dequeue queue =
    inl {from to ar} = queue
    assert (from <> to) "Cannot dequeue past the end of the queue."
    queue.from <- add_one (array_length ar) from
    ar from

inl create typ n =
    inl n = match n with () -> 16 | n -> max 1 n
    heapm {from=dyn 0; to=dyn 0; ar=array_create typ n}

// Note: By convention I am disallowing module methods to keep track of their data.
// Hence the user will have to use enqueue and dequeue statically on the queue.

// This is because {queue with queue = new_queue} will not update the queue and dequeue
// which will still point to the old and unused data. Modules are not to be used as classes.

// Closures can be used as classes as their fields cannot be updated immutably.
{create enqueue dequeue}
|> stackify
    """) |> module_

let struct' = 
    (
    "Struct",[],"The Struct module.",
    """
// The functions in this module are intended for iterating over generic types.
// The modules always iterate over the first argument to them.
inl map' f x = 
    inl rec loop = function
        | x when caseable_box_is x -> f x
        | x :: xs -> loop x :: loop xs
        | () -> ()
        | {} & x -> module_map (inl _ -> loop) x
        | x -> f x
    loop x

inl rec is_empty = function
    | x when caseable_box_is x -> false
    | x :: xs -> is_empty x && is_empty xs
    | () -> true
    | {} & x -> module_foldl (inl k s x -> s && is_empty x) true x
    | x -> false

inl map f x = 
    inl rec loop = function
        | x when caseable_box_is x -> f x
        | x :: xs -> loop x :: loop xs
        | () -> ()
        | {!block} & x -> module_map (inl _ -> loop) x
        | x -> f x
    loop x

inl map2' f a b = 
    inl rec loop = function
        | x, y when caseable_box_is x || caseable_box_is y -> f x y
        | x :: xs, y :: ys -> loop (x,y) :: loop (xs,ys)
        | (), () -> ()
        | {} & x, {} & y -> module_map (inl k x -> loop (x,y k)) x
        | x, y -> f x y
    loop (a,b)

inl map2 f a b = 
    inl rec loop = function
        | x, y when caseable_box_is x || caseable_box_is y -> f x y
        | x :: xs, y :: ys -> loop (x,y) :: loop (xs,ys)
        | (), () -> ()
        | {!block} & x, {!block} & y -> module_map (inl k x -> loop (x,y k)) x
        | x, y -> f x y
    loop (a,b)

inl map3' f a b c = 
    inl rec loop = function
        | x, y, z when caseable_box_is x || caseable_box_is y || caseable_box_is z -> f x y z
        | x :: xs, y :: ys, z :: zs -> loop (x,y,z) :: loop (xs,ys,zs)
        | (), (), () -> ()
        | {} & x, {} & y, {} & z -> module_map (inl k x -> loop (x,y k,z k)) x
        | x, y, z -> f x y z
    loop (a,b,c)

inl map3 f a b c = 
    inl rec loop = function
        | x, y, z when caseable_box_is x || caseable_box_is y || caseable_box_is z -> f x y z
        | x :: xs, y :: ys, z :: zs -> loop (x,y,z) :: loop (xs,ys,zs)
        | (), (), () -> ()
        | {!block} & x, {!block} & y, {!block} & z -> module_map (inl k x -> loop (x,y k,z k)) x
        | x, y, z -> f x y z
    loop (a,b,c)

inl iter f = map (inl x -> f x; ()) >> ignore
inl iter2 f a b = map2 (inl a b -> f a b; ()) a b |> ignore
inl iter3 f a b c = map3 (inl a b c -> f a b c; ()) a b c |> ignore

inl foldl f s x = 
    inl rec loop s = function
        | x when caseable_box_is x -> f s x
        | () -> s
        | x :: xs -> loop (loop s x) xs
        | {!block} & x -> module_foldl (inl _ -> loop) s x
        | x -> f s x
    loop s x

inl foldl2 f s a b = 
    inl rec loop s = function
        | x, y when caseable_box_is x || caseable_box_is y -> f s x y
        | x :: xs, y :: ys -> loop (loop s (x,y)) (xs,ys)
        | (), () -> s
        | {!block} & x, {!block} & y -> module_foldl (inl k s x -> loop s (x,y k)) s x
        | x, y -> f s x y
    loop s (a,b)

inl foldl3 f s a b c = 
    inl rec loop s = function
        | x, y, z when caseable_box_is x || caseable_box_is y || caseable_box_is z -> f s x y z
        | x :: xs, y :: ys, z :: zs -> loop (loop s (x,y,z)) (xs,ys,zs)
        | (), (), () -> s
        | {!block} & x, {!block} & y, {!block} & z -> module_foldl (inl k s x -> loop s (x,y k,z k)) s x
        | x, y, z -> f s x y z
    loop s (a,b,c)

inl choose f x = 
    inl rec loop = function
        | x when caseable_box_is x -> f x
        | () -> ()
        | x :: xs -> 
            match loop x with
            | () -> loop xs
            | x -> x :: loop xs
        | {!block} & x -> 
            module_foldl (inl k s x -> 
                match loop x with
                | () -> s
                | x -> module_add k x s
                ) {} x
        | x -> f x
    loop x

inl choose2 f a b = 
    inl rec loop = function
        | x, y when caseable_box_is x || caseable_box_is y -> f x y
        | x :: xs, y :: ys ->
            match loop (x,y) with
            | () -> loop (xs,ys)
            | x -> x :: loop (xs,ys)
        | (), () -> ()
        | {!block} & x, {!block} & y ->
            module_foldl (inl k s x -> 
                match loop (x,y k) with
                | () -> s
                | x -> module_add k x s
                ) {} x
        | x, y -> f x y
    loop (a,b)

inl choose3 f a b c = 
    inl rec loop = function
        | x, y, z when caseable_box_is x || caseable_box_is y || caseable_box_is z -> f x y z
        | x :: xs, y :: ys, z :: zs -> 
            match loop (x,y,z) with
            | () -> loop (xs,ys,zs)
            | x -> x :: loop (xs,ys,zs)
        | (), (), () -> ()
        | {!block} & x, {!block} & y, {!block} & z -> 
            module_foldl (inl k s x -> 
                match loop (x,y k,z k) with
                | () -> s
                | x -> module_add k x s
                ) {} x
        | x, y, z -> f x y z
    loop (a,b,c)

inl foldl_map f s x = 
    inl rec loop s = function
        | x when caseable_box_is x -> f s x
        | () -> (), s
        | x :: xs -> 
            inl x, s = loop s x
            inl x', s = loop s xs
            x :: x', s
        | {!block} & x -> 
            module_foldl (inl k (m,s) v -> 
                inl x, s = loop s v
                inl m = module_add k x m
                m, s
                ) ({},s) x
        | x -> f s x
    loop s x

inl foldl2_map f s a b = 
    inl rec loop s = function
        | x, y when caseable_box_is x || caseable_box_is y -> f s x y
        | x :: xs, y :: ys -> 
            inl x, s = loop s (x,y)
            inl x', s = loop s (xs,ys)
            x :: x', s
        | (), () -> (), s
        | {!block} & x, {!block} & y -> 
            module_foldl (inl k (m,s) v -> 
                inl x, s = loop s (v, y k)
                inl m = module_add k x m
                m, s
                ) ({},s) x
        | x, y -> f s x y
    loop s (a,b)

inl foldl3_map f s a b c = 
    inl rec loop s = function
        | x, y, z when caseable_box_is x || caseable_box_is y || caseable_box_is z -> f s x y z
        | x :: xs, y :: ys, z :: zs -> 
            inl x, s = loop s (x,y,z)
            inl x', s = loop s (xs,ys,zs)
            x :: x', s
        | (), (), () -> (), s
        | {!block} & x, {!block} & y, {!block} & z ->
            module_foldl (inl k (m,s) v -> 
                inl x, s = loop s (v, y k, z k)
                inl m = module_add k x m
                m, s
                ) ({},s) x
        | x, y, z -> f s x y z
    loop s (a,b,c)

inl foldr f a s = 
    inl rec loop s = function
        | x when caseable_box_is x -> f x s
        | x :: xs -> loop (loop s xs) x
        | () -> s
        | {!block} & x -> module_foldr (inl k x s -> loop s x) x s
        | x -> f x s
    loop s a

inl foldr2 f a b s = 
    inl rec loop s = function
        | x, y when caseable_box_is x || caseable_box_is y -> f x y s
        | x :: xs, y :: ys -> loop (loop s (xs,ys)) (x,y)
        | (), () -> s
        | {!block} & x, {!block} & y -> module_foldr (inl k x s -> loop s (x,y k)) x s
        | x, y -> f x y s
    loop s (a,b)

inl foldr3 f a b c s = 
    inl rec loop s = function
        | x, y, z when caseable_box_is x || caseable_box_is y || caseable_box_is z -> f x y z s
        | x :: xs, y :: ys, z :: zs -> loop (loop s (xs,ys,zs)) (x,y,z)
        | (), (), () -> s
        | {!block} & x, {!block} & y, {!block} & z -> module_foldr (inl k x s -> loop s (x,y k,z k)) x s
        | x, y, z -> f x y z s
    loop s (a,b,c)

inl foldr_map f a s = 
    inl rec loop s = function
        | x when caseable_box_is x -> f x s
        | x :: xs -> 
            inl x', s = loop s xs
            inl x, s = loop s x
            x :: x', s
        | () -> (), s
        | {!block} & x ->
            module_foldr (inl k v (m,s) -> 
                inl x, s = loop s v
                inl m = module_add k x m
                m, s
                ) x ({},s)
        | x -> f x s
    loop s a

inl foldr2_map f a b s = 
    inl rec loop s = function
        | x, y when caseable_box_is x || caseable_box_is y -> f x y s
        | x :: xs, y :: ys -> 
            inl x', s = loop s (xs,ys)
            inl x, s = loop s (x,y)
            x :: x', s
        | (), () -> (), s
        | {!block} & x, {!block} & y ->
            module_foldr (inl k v (m,s) -> 
                inl x, s = loop s (v, y k)
                inl m = module_add k x m
                m, s
                ) x ({},s)
        | x, y -> f x y s
    loop s (a,b)

inl foldr3_map f a b c s = 
    inl rec loop s = function
        | x, y, z when caseable_box_is x || caseable_box_is y || caseable_box_is z -> f x y z s
        | x :: xs, y :: ys, z :: zs -> 
            inl x', s = loop s (xs,ys,zs)
            inl x, s = loop s (x,y,z)
            x :: x', s
        | (), (), () -> (), s
        | {!block} & x, {!block} & y, {!block} & z ->
            module_foldr (inl k v (m,s) -> 
                inl x, s = loop s (v, y k, z k)
                inl m = module_add k x m
                m, s
                ) x ({},s)
        | x, y, z -> f x y z s
    loop s (a,b,c)

{
map' map map2' map2 map3' map3 iter iter2 iter3 foldl foldl2 foldl3 choose choose2 choose3 
foldl_map foldl2_map foldl3_map foldr foldr2 foldr3 foldr_map foldr2_map foldr3_map is_empty
} |> stackify
    """) |> module_

let host_tensor =
    (
    "HostTensor",[tuple;struct';loops;extern_;console],"The host tensor module.",
    """
// A lot of the code in this module is made with purpose of being reused on the Cuda side.
inl map_dim = function
    | {from to} -> 
        assert (from <= to) "Tensor needs to be at least size 1."
        {from; near_to=to+1}
    | {from near_to} as d -> 
        assert (from < near_to) "Tensor needs to be at least size 1."
        d
    | x -> 
        assert (x > 0) "Tensor needs to be at least size 1."
        {from=0; near_to=x}

inl map_dims = Tuple.map map_dim << Tuple.wrap

inl span = function
    | {from near_to} -> near_to - from
    | {from by} -> by
    | {from to} -> to - from + 1
    | x : int64 -> x

inl rec view_offsets offset = function
    | s :: s', i :: i' -> s * i + view_offsets offset (s', i')
    | _, () -> offset

inl tensor_view {data with size offset} i' = {data with offset = view_offsets offset (size,i')}
inl tensor_get {data with offset ar} = ar offset
inl tensor_set {data with offset ar} v = ar offset <- v
inl tensor_apply {data with size=s::size offset} i = {data with size offset=offset + i * s}
inl tensor_update_dim f dim = dim |> Tuple.map span |> Tuple.unwrap |> f

inl show' {cutoff_near_to} tns = 
    open Extern
    inl strb_type = fs [text: "System.Text.StringBuilder"]
    inl s = FS.Constructor strb_type ()
    inl append x = FS.Method s .Append x strb_type |> ignore
    inl append_line x = FS.Method s .AppendLine x strb_type |> ignore
    inl indent near_to = Loops.for {from=0; near_to; body=inl _ -> append ' '}
    inl blank = dyn ""
    inl rec loop {tns ind cutoff} =
        match tns.dim with
        | () -> tns.get |> Extern.show |> append
        | {from near_to} :: () ->
            indent ind; append "[|"
            inl cutoff =
                Loops.for' {from near_to state=blank,cutoff; finally=snd; body=inl {next state=prefix,cutoff i} -> 
                    if cutoff < cutoff_near_to then
                        append prefix
                        tns i .get |> Extern.show |> append
                        next (dyn "; ", cutoff+1)
                    else
                        append "..."
                        cutoff
                    }
            append_line "|]"
            cutoff
        | {from near_to} :: x' ->
            indent ind; append_line "[|"
            inl cutoff =
                Loops.for' {from near_to state=cutoff; body=inl {next state=cutoff i} -> 
                    if cutoff < cutoff_near_to then
                        loop {tns=tns i; ind=ind+4; cutoff} |> next
                    else
                        indent ind; append_line "..."
                        cutoff
                    }
            indent ind; append_line "|]"
            cutoff

    loop {tns; ind=0; cutoff=dyn 0} |> ignore
    FS.Method s .ToString() string

inl show = show' {cutoff_near_to=1000}

/// Total tensor size in elements.
inl length = Tuple.foldl (inl s (!span x) -> s * x) 1 << Tuple.wrap

/// Splits a tensor's dimensions. Works on non-contiguous tensors.
/// Given the tensor dimensions (a,b,c) and a function which maps them to (a,(q,w),c)
/// the resulting tensor dimensions come out to (a,q,w,c).
inl split f tns =
    inl rec concat = function
        | d :: d', n :: n' -> 
            inl next = concat (d',n')
            match n with
            | _ :: _ -> 
                assert (span d = length n) "The length of the split dimension must equal to that of the previous one."
                Tuple.append n next
            | _ -> 
                assert (span d = span n) "The span on the new dimension must be equal to that of the previous one."
                n :: next
        | d', () -> d'

    inl rec update_size = function
        | init :: s', dim :: x' ->
            inl next = update_size (s', x')
            match dim with
            | _ :: _ ->
                inl _ :: size = Tuple.scanr (inl x s -> span x * s) dim init
                Tuple.append size next
            | _ -> init :: next
        | s', () -> s'

    match tns.dim with
    | () ->
        inl f = Tuple.wrap f
        assert (length f = 1) "The length of new dimensions for the scalar tensor must be 1."
        tns .set_dim f .update_body (inl d -> {d with size=f})
    | dim ->
        inl dim' =
            inl rec wrapped_is = function
                | (_ :: _) :: _ -> true
                | _ :: x' -> wrapped_is x'
                | _ -> false
            
            match dim with
            | dim :: () -> span dim
            | dim -> Tuple.map span dim
            |> f |> inl x -> if wrapped_is x then x else x :: ()

        tns .set_dim (concat (dim, dim'))
            .update_body (inl d -> {d with size=update_size (self,dim')})

/// Flattens the tensor to a single dimension.
inl flatten tns =
    match tns.dim with
    | () -> tns
    | !(Tuple.map span) dim ->
        tns .set_dim (length dim)
            .update_body (inl {d with size} ->
                Tuple.zip (dim,size)
                |> Tuple.reducel (inl d,s d',s' ->
                    assert (s = d' * s') "The tensor must be contiguous in order to be flattened."
                    d*s, s'
                    )
                |> inl _,s -> {d with size=s :: ()}
                )

/// Flattens and then splits the tensor dimensions.
inl reshape f tns = split (inl _ -> tns.dim |> Tuple.map span |> Tuple.unwrap |> f) (flatten tns)

inl view {data with dim} f =
    inl rec new_dim = function
        | {from near_to} :: d', {nd with from=from' near_to=near_to'} :: h' ->
            assert (from' >= from && from' < near_to) "Lower boundary out of bounds." 
            assert (near_to' > from && near_to' <= near_to) "Higher boundary out of bounds." 
            inl i',nd' = new_dim (d',h')
            from'-from :: i', nd :: nd'
        | (), _ :: _ -> error_type "The view has more dimensions than the tensor."
        | dim, () -> (),dim

    inl indices, dim = new_dim (dim, tensor_update_dim f dim |> map_dims)
    {data with bodies = Struct.map (inl ar -> tensor_view ar indices) self; dim}

inl view_span {data with dim} f =
    inl rec new_dim = function
        | {from near_to} :: d', h :: h' ->
            inl check from' near_to' =
                assert (from' >= from && from' < near_to) "Lower boundary out of bounds." 
                assert (near_to' > from && near_to' <= near_to) "Higher boundary out of bounds." 
            inl case_from_near_to {nd with from=from' near_to=near_to'} =
                inl from' = from + from'
                check from' (from + near_to')
                from', {from = 0; near_to = span nd}

            inl i, nd = 
                match h with
                | {from=from' by} ->
                    assert (by >= 0) "`by` must be positive or zero."
                    inl from' = from + from'
                    check from' (from' + by)
                    from', {from = 0; near_to = by}
                // Note: Do not remove from' or it will shadow it in the next branch.
                | {from=from' near_to} -> case_from_near_to h
                | {from=from'} ->
                    inl from = from + from'
                    check from near_to
                    from', {from = 0; near_to = span {from near_to}}
                | _ -> case_from_near_to (map_dim h)
            inl i', nd' = new_dim (d',h')
            i :: i', nd :: nd'
        | (), _ :: _ -> error_type "The view has more dimensions than the tensor."
        | dim, () -> (),dim

    inl indices, dim = new_dim (dim, tensor_update_dim f dim |> Tuple.wrap)
    {data with bodies = Struct.map (inl ar -> tensor_view ar indices) self; dim}

inl rec facade data = 
    inl methods = stack {
        length = inl {data with dim} -> length dim
        elem_type = inl {data with bodies} -> Struct.map (inl {ar} -> ar.elem_type) bodies
        update_body = inl {data with bodies} f -> {data with bodies=Struct.map f bodies} |> facade
        set_dim = inl {data with dim} dim -> {data with dim=map_dims dim} |> facade
        get = inl {data with dim bodies} -> 
            match dim with
            | () -> Struct.map tensor_get bodies
            | _ -> error_type "Cannot get from tensor whose dimensions have not been applied completely."
        set = inl {data with dim bodies} v ->
            match dim with
            | () -> Struct.iter2 (inl v bodies -> tensor_set bodies v) v bodies
            | _ -> error_type "Cannot set to a tensor whose dimensions have not been applied completely."
        // Crops the dimensions of a tensor.
        view = inl data -> view data >> facade
        // Resizes the view towards zero.
        view_span = inl data -> view_span data >> facade
        /// Applies the tensor. `i` can be a tuple.
        apply = inl data i ->
            Tuple.foldl (inl {data with dim} i ->
                match dim with
                | () -> error_type "Cannot apply the tensor anymore."
                | {from near_to} :: dim ->
                    assert (i >= from && i < near_to) "Argument out of bounds." 
                    {data with bodies = Struct.map (inl ar -> tensor_apply ar (i-from)) self; dim}
                ) data (Tuple.wrap i)
            |> facade
        /// Returns the tensor data.
        unwrap = id
        /// Returns an empty tensor of the same dimension.
        empty = inl data -> facade {data with bodies=()}
        span_outer = inl {dim} -> match dim with () -> 1 | x :: _ -> span x
        span_outer2 = inl {dim=a::b::_} -> span a * span b
        span_outer3 = inl {dim=a::b::c::_} -> span a * span b * span c
        split = inl data f -> split f (facade data)
        flatten = inl data -> flatten (facade data)
        reshape = inl data f -> reshape f (facade data)
        from = inl {dim={from}::_} -> from
        near_to = inl {dim={near_to}::_} -> near_to
        // Rounds the dimension to the multiple.
        round = inl data mult -> view_span data (inl x :: _ | x -> x - x % mult) |> facade
        // Rounds the dimension to a multiple and splits it so that the outermost dimension becomes the multiple.
        round_split = inl data mult -> facade data .round mult .split (inl x :: _ | x -> mult,x/mult)
        // Rounds the dimension to a multiple and splits it so that the dimension next to the outermost becomes the multiple.
        round_split' = inl data mult -> facade data .round mult .split (inl x :: _ | x -> x/mult,mult)
        }

    function
    | .(_) & x -> 
        if module_has_member x data then data x
        else methods x data
    | i -> methods .apply data i

inl make_body {d with dim elem_type} =
    match dim with
    | () -> error_type "Empty dimensions are not allowed."
    | dim ->
        inl init =
            match d with
            | {pad_to} -> min 1 (pad_to / sizeof elem_type)
            | {last_size} -> last_size
            | _ -> 1
        inl len :: size = Tuple.scanr (inl (!span x) s -> x * s) dim init
        inl ar = match d with {array_create} | _ -> array_create elem_type len
        {ar size offset=0; block=()}

/// Creates an empty tensor given the descriptor. {size elem_type ?layout=(.toa | .aot) ?array_create ?pad_to} -> tensor
inl create {dsc with dim elem_type} = 
    inl create (!map_dims dim) =
        inl dsc = {dsc with dim}
        inl bodies =
            inl layout = match dsc with {layout} -> layout | _ -> .toa
            inl create elem_type = make_body {dsc with elem_type}
            match layout with
            | .aot -> create elem_type
            | .toa -> Struct.map create elem_type

        facade {bodies dim}
    match dim with
    | () -> create 1 0
    | dim -> create dim
    
/// Creates a new tensor based on given sizes. Takes in a setter function. 
/// ?layout -> size -> f -> tensor.
inl init = 
    inl body layout size f = 
        inl dim = Tuple.wrap size
        inl elem_type = type (Tuple.foldl (inl f _ -> f 0) f dim)
        inl tns = create {elem_type dim layout}
        inl rec loop tns f = 
            match tns.dim with
            | {from near_to} :: _ -> Loops.for { from near_to; body=inl {i} -> loop (tns i) (f i) }
            | () -> tns .set f
        loop tns f
        tns
    function
    | .aot  -> body .aot
    | size -> body .toa size

/// Maps the elements of the tensor. (a -> b) -> a tensor -> b tensor
inl map f tns =
    inl size = tns.dim
    inl rec loop tns = function
        | _ :: x' -> inl x -> loop (tns x) x' 
        | () -> f (tns .get)
    
    init size (loop tns size)

/// Copies a tensor. tensor -> tensor
inl copy = map id

/// Asserts the tensor size. Useful for setting those values to statically known ones. 
/// Should be used on 1d tensors. Does not copy. size -> tensor -> tensor.
inl assert_size (!map_dims dim') tns = 
    assert (tns.dim = dim') "The dimensions must match."
    tns.set_dim dim'

/// Reinterprets an array as a tensor. Does not copy. array -> tensor.
inl array_as_tensor ar = facade {dim=map_dims (array_length ar); bodies={ar size=1::(); offset=0; block=()}}

/// Reinterprets an array as a tensor. array -> tensor.
inl array_to_tensor = array_as_tensor >> copy

/// Asserts that all the dimensions of the tensors are equal. Returns the dimension of the first tensor if applicable.
/// tensor structure -> (tensor | ())
inl assert_zip l =
    Struct.foldl (inl s x ->
        match s with
        | () -> x
        | s -> assert (s.dim = x.dim) "All tensors in zip need to have the same dimensions"; s) () l

/// Zips all the tensors in the argument together. Their dimensions must be equal.
/// tensor structure -> tensor
inl zip l = 
    match assert_zip l with
    | () -> error_type "Empty inputs to zip are not allowed."
    | !(inl x -> x.unwrap) tns -> facade {tns with bodies=Struct.map (inl x -> x.bodies) l}

/// Unzips all the elements of a tensor.
/// tensor -> tensor structure
inl unzip tns =
    inl {bodies dim} = tns.unwrap
    Struct.map (inl bodies -> facade {bodies dim}) bodies

/// Are all subtensors structurally equal?
/// tensor structure -> bool
inl rec equal (!zip t) =
    match t.dim with
    | {from near_to} :: _ ->
        Loops.for' {from near_to state=true; body=inl {next i} ->
            equal (t i) && next true
            }
    | _ -> 
        inl a :: b = t.get
        Tuple.forall ((=) a) b

/// Asserts that the tensor is contiguous.
inl assert_contiguous = flatten >> ignore
/// Asserts that the dimensions of the tensors are all equal.
inl assert_dim l = assert_zip >> ignore
/// Prints the tensor to the standard output.
met print (!dyn x) = 
    match x with
    | {cutoff input} -> show' {cutoff_near_to=cutoff} input
    | x -> show x 
    |> Console.writeline

/// Creates a tensor from a scalar.
/// x -> x tensor
inl from_scalar x =
    inl t = create {dim=(); elem_type=type x}
    t .set x
    t

{
create facade init copy assert_size array_as_tensor array_to_tensor map zip show print length
span equal split flatten assert_contiguous assert_dim reshape unzip from_scalar map_dim map_dims
} |> stackify
    """) |> module_

let object =
    (
    "Object",[],"The Object module.",
    """
{
data' = {}
data = inl {data'} -> data'
data_add = inl s v -> {s with data'=module_foldl (inl name s v -> module_add name v s) (indiv self) v |> heap} |> obj
member_add = inl s -> module_foldl (inl name s v -> module_add name (inl s -> v (obj s)) s) s >> obj
module_add = inl s name v -> module_add name (inl s name -> v name (obj s)) s |> obj
unwrap = id
} 
|> obj
    """) |> module_

let cuda =
    (
    "Cuda",[loops;console;array;host_tensor;extern_;object],"The Cuda module.",
    """
inl ret -> 
    open Extern
    open Console

    inl cuda_kernels = FS.Constant.cuda_kernels string

    inl cuda_constant a t = !MacroCuda(t,[text: a])

    inl cuda_constant_int constant () = cuda_constant constant int64

    inl __blockDimX = cuda_constant_int "blockDim.x"
    inl __blockDimY = cuda_constant_int "blockDim.y"
    inl __blockDimZ = cuda_constant_int "blockDim.z"
    inl __gridDimX = cuda_constant_int "gridDim.x"
    inl __gridDimY = cuda_constant_int "gridDim.y"
    inl __gridDimZ = cuda_constant_int "gridDim.z"

    inl cub_path = @CubPath
    inl cuda_toolkit_path = @CudaPath
    inl nvcc_options = @CudaNVCCOptions
    inl visual_studio_path = @VSPath
    inl vs_path_vcvars = @VSPathVcvars
    inl vcvars_args = @VcvarsArgs
    inl vs_path_cl = @VSPathCL
    inl vs_path_include = @VSPathInclude

    inl env_type = fs [text: "System.Environment"]
    inl context_type = fs [text: "ManagedCuda.CudaContext"]
    use context = FS.Constructor context_type false
    FS.Method context .Synchronize() ()

    inl compile_kernel_using_nvcc_bat_router (kernels_dir: string) =
        inl path_type = fs [text: "System.IO.Path"]
        inl combine x = FS.StaticMethod path_type .Combine x string
    
        inl file_type = fs [text: "System.IO.File"]
        inl stream_type = fs [text: "System.IO.Stream"]
        inl streamwriter_type = fs [text: "System.IO.StreamWriter"]
        inl process_start_info_type = fs [text: "System.Diagnostics.ProcessStartInfo"]
    
        inl nvcc_router_path = combine (kernels_dir,"nvcc_router.bat")
        inl procStartInfo = FS.Constructor process_start_info_type ()
        FS.Method procStartInfo .set_RedirectStandardOutput true ()
        FS.Method procStartInfo .set_RedirectStandardError true ()
        FS.Method procStartInfo .set_UseShellExecute false ()
        FS.Method procStartInfo .set_FileName nvcc_router_path ()

        inl process_type = fs [text: "System.Diagnostics.Process"]
        use process = FS.Constructor process_type ()
        FS.Method process .set_StartInfo procStartInfo ()
        inl print_to_standard_output = 
            closure_of (inl args -> FS.Method args .get_Data() string |> writeline) 
                (fs [text: "System.Diagnostics.DataReceivedEventArgs"] => ())

        FS.Method process ."OutputDataReceived.Add" print_to_standard_output ()
        FS.Method process ."ErrorDataReceived.Add" print_to_standard_output ()

        inl concat = string_concat ""
        inl (+) a b = concat (a, b)

        /// Puts quotes around the string.
        inl quote x = ("\"",x,"\"")

        inl quoted_vs_path_to_vcvars = combine(visual_studio_path, vs_path_vcvars) |> quote
        inl quoted_vs_path_to_cl = combine(visual_studio_path, vs_path_cl) |> quote
        inl quoted_vc_path_to_include = combine(visual_studio_path, vs_path_include) |> quote
        inl quoted_cuda_toolkit_path_to_include = combine(cuda_toolkit_path,"include") |> quote
        inl quoted_nvcc_path = combine(cuda_toolkit_path,@"bin/nvcc.exe") |> quote
        inl quoted_cub_path_to_include = cub_path |> quote
        inl quoted_kernels_dir = kernels_dir |> quote
        inl target_path = combine(kernels_dir,"cuda_kernels.ptx")
        inl quoted_target_path = target_path |> quote
        inl input_path = combine(kernels_dir,"cuda_kernels.cu")
        inl quoted_input_path = input_path |> quote

        if FS.StaticMethod file_type .Exists input_path bool then FS.StaticMethod file_type .Delete input_path ()
        FS.StaticMethod file_type .WriteAllText(input_path,cuda_kernels) ()
   
        inl _ = 
            if FS.StaticMethod file_type .Exists nvcc_router_path bool then FS.StaticMethod file_type .Delete nvcc_router_path ()
            inl filestream_type = fs [text: "System.IO.FileStream"]

            use nvcc_router_file = FS.StaticMethod file_type .OpenWrite(nvcc_router_path) filestream_type
            use nvcc_router_stream = FS.Constructor streamwriter_type nvcc_router_file

            inl write_to_batch = concat >> inl x -> FS.Method nvcc_router_stream .WriteLine x ()

            "SETLOCAL" |> write_to_batch
            ("CALL ", quoted_vs_path_to_vcvars, vcvars_args) |> write_to_batch
            ("SET PATH=%PATH%;", quoted_vs_path_to_cl) |> write_to_batch
            (
            quoted_nvcc_path, " ", nvcc_options, " --use-local-env --cl-version 2017",
            " -I", quoted_cuda_toolkit_path_to_include,
            " -I", quoted_cub_path_to_include,
            " -I", quoted_vc_path_to_include,
            " --keep-dir ",quoted_kernels_dir,
            " -maxrregcount=0  --machine 64 -ptx -cudart static  -o ",quoted_target_path," ",quoted_input_path
            ) |> write_to_batch

        inl stopwatch_type = fs [text: "System.Diagnostics.Stopwatch"]
        inl timer = FS.StaticMethod stopwatch_type .StartNew () stopwatch_type
        if FS.Method process .Start() bool = false then failwith () "NVCC failed to run."
        FS.Method process .BeginOutputReadLine() ()
        FS.Method process .BeginErrorReadLine() ()
        FS.Method process .WaitForExit() ()

        inl exit_code = FS.Method process .get_ExitCode() int32
        assert (exit_code = 0i32) ("NVCC failed compilation.", exit_code)
    
        inl elapsed = FS.Method timer .get_Elapsed() (fs [text: "System.TimeSpan"])
        !MacroFs((),[text: "printfn \"The time it took to compile the Cuda kernels is: %A\" "; arg: elapsed])

        FS.Method context .LoadModulePTX target_path (fs [text: "ManagedCuda.BasicTypes.CUmodule"])

    inl current_directory = FS.StaticMethod env_type .get_CurrentDirectory() string
    inl modules = compile_kernel_using_nvcc_bat_router current_directory
    writeline (string_concat "" ("Compiled the kernels into the following directory: ", current_directory))

    inl dim3 = function
        | {} as m ->
            inl m = match m with {x=x: int64} -> m | _ -> {m with x=1}
            inl m = match m with {y=y: int64} -> m | _ -> {m with y=1}
            match m with {z=z: int64} -> m | _ -> {m with z=1}
        | z,y,x -> {x=x: int64; y=y: int64; z=z: int64}
        | y,x -> {x=x: int64; y=y: int64; z=1}
        | x -> {x=x: int64; y=1; z=1}

    inl SizeT_type = fs [text: "ManagedCuda.BasicTypes.SizeT"]
    inl CUdeviceptr_type = fs [text: "ManagedCuda.BasicTypes.CUdeviceptr"]
    inl SizeT = FS.Constructor SizeT_type
    inl CUdeviceptr = FS.Constructor CUdeviceptr_type << SizeT

    met run s {blockDim gridDim kernel} =
        inl blockDim = dim3 blockDim
        inl gridDim = dim3 gridDim
        inl to_obj_ar args =
            inl ty = fs [text: "System.Object"] |> array
            !MacroFs(ty,[fs_array_args: args; text: ": "; type: ty])

        inl kernel =
            inl map_to_op_if_not_static {x y z} (x', y', z') = 
                inl f x x' = if lit_is x then const x else x' 
                f x x', f y y', f z z'
            inl x,y,z = map_to_op_if_not_static blockDim (__blockDimX,__blockDimY,__blockDimZ)
            inl x',y',z' = map_to_op_if_not_static gridDim (__gridDimX,__gridDimY,__gridDimZ)
            inl _ -> // This convoluted way of swaping non-literals for ops is so they do not get called outside of the kernel.
                inl blockDim = {x=x(); y=y(); z=z()}
                inl gridDim = {x=x'(); y=y'(); z=z'()}
                kernel blockDim gridDim

        inl join_point_entry_cuda x = !JoinPointEntryCuda(x())
        inl method_name, args = join_point_entry_cuda kernel
        
        inl dim3 {x y z} = Tuple.map (to uint32) (x,y,z) |> FS.Constructor (fs [text: "ManagedCuda.VectorTypes.dim3"])
    
        inl kernel_type = fs [text: "ManagedCuda.CudaKernel"]
        inl cuda_kernel = FS.Constructor kernel_type (method_name,modules,s.data.context)
        FS.Method cuda_kernel .set_GridDimensions(dim3 gridDim) ()
        FS.Method cuda_kernel .set_BlockDimensions(dim3 blockDim) ()

        match s.data.stream with
        | () -> FS.Method cuda_kernel .Run(to_obj_ar args) float32
        | stream -> FS.Method cuda_kernel .RunAsync(stream.extract,to_obj_ar args) ()
        
    inl synchronize s = FS.Method s.data.context .Synchronize() ()

    Object
        .member_add {run synchronize}
        .data_add {context stream=()}
    |> ret
    """) |> module_

let random =
    (
    "Random",[],"The Random module.",
    """
/// Wrapper for the standard .NET Random class.
stack inl seed ->
    inl ty = fs [text: "System.Random"]
    inl rnd = match seed with _ : int32 | () -> macro.fs ty [type: ty; args: seed]
        
    inl next = inl ((min : int32, max : int32) | (max : int32) | () as x) -> macro.fs int32 [arg: rnd; text: ".Next"; args: x]
    inl next_double = inl _ -> macro.fs float64 [arg: rnd; text: ".NextDouble()"]
    inl next_bytes = inl (ar: (array uint8)) -> macro.fs () [arg: rnd; text: ".NextBytes"; args: ar]
    function
    | .next -> next
    | .next_double -> next_double()
    | .next_bytes -> next_bytes
    |> stack
    """) |> module_

let math = 
    (
    "Math",[],"The Math module.",
    """
/// The partially evaluated power function using repeated squaring.
/// Note that the second argument for the function needs to be an integer.
/// The language already has an inbuilt operator ** for floats as the second argument.
inl rec pow (n: float64 | n: float32) (k: int64) =
    if k < 0 then to n 1 / pow n -k
    elif k=1 then n
    elif k=0 then to n 1
    else
        inl rec pow n k =
            inl body () =
                if k = 2 then n*n
                elif k % 2 = 1 then n * pow n (k-1)
                else 
                    inl x = pow n (k/2)
                    x*x
        
            if lit_is k then body ()
            else join body() : n

        pow n k

{pow} |> stackify
    """) |> module_