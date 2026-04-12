# Surreal

A C# implementation of [Conway's surreal numbers](https://en.wikipedia.org/wiki/Surreal_number) — a number system that unifies integers, fractions, irrationals, infinitesimals, and transfinite ordinals under a single recursive definition.

## The number line

This library can represent and compare numbers across the entire surreal number line:

```
0 < 1/ω < 1/1000 < 1/5 < 1/3 < 1/2 < 1 < √2 < π < 100
  < √ω < ω/2 < ω < 2ω < 3ω < ω² < ω³ < ω^ω
```

From positive infinitesimals (1/ω) through all real numbers (rational, irrational, transcendental) to transfinite ordinals (ω, ω², ω^ω).

## Quick start

```bash
dotnet build
dotnet run           # runs Starter.cs demo
dotnet test          # runs 168 tests
```

## Usage

```csharp
// Integers
var three = new Surr(3);
var seven = new Surr(7);
Console.WriteLine(three + seven);     // 10
Console.WriteLine(three * seven);     // 21

// Dyadic fractions (n/2^k)
var half = Surr.Half;                 // 1/2
var quarter = Surr.Dyadic(1, 2);     // 1/4
Console.WriteLine(half * half);       // 1/4
Console.WriteLine(quarter * 4);       // 1

// Non-dyadic rationals
var third = Surr.FromRational(1, 3);
Console.WriteLine(third < half);      // True
Console.WriteLine(third == Surr.FromRational(2, 6));  // True

// Irrational numbers
var sqrt2 = Surr.FromSqrt(2);
Console.WriteLine(sqrt2 * sqrt2);     // 2
Console.WriteLine(sqrt2 > Surr.FromRational(7, 5));   // True (√2 > 1.4)

// Transcendental numbers
var pi = Surr.Pi();
Console.WriteLine(pi > 3);            // True
Console.WriteLine(pi < Surr.FromRational(3142, 1000)); // True

// Transfinite numbers
var w = Surr.Omega;                   // ω = {0,1,2,...|}
Console.WriteLine(w > 1000000);       // True
Console.WriteLine(Surr.SqrtOmega * Surr.SqrtOmega == w);  // True

// Infinitesimals
var eps = Surr.InverseOmega;          // 1/ω = {0|1,1/2,1/4,...}
Console.WriteLine(eps > 0);           // True
Console.WriteLine(eps < quarter);     // True (smaller than any positive real)

// Algebraic identities work across number types
var sw = Surr.SqrtOmega;
var s2 = Surr.FromSqrt(2);
Console.WriteLine((sw - s2) * (sw + s2) + new Surr(2) == w);  // True
// (√ω - √2)(√ω + √2) + 2 = ω  ✓

// Custom Dedekind cuts
var phi = Surr.FromPredicate(
    (mid, exp) => mid * mid < (mid + (1L << exp)) * (1L << exp),
    1, "φ");  // golden ratio via mid² < mid + 1
```

## How it works

Every surreal number is a pair `{L | R}` where L (left options) and R (right options) are sets of previously-created surreal numbers, with every element of L less than every element of R. The value is the "simplest" number that fits between L and R.

### Comparison

All comparisons use Conway's recursive definition:

> **a ≤ b** iff no left option of a is ≥ b, and no right option of b is ≤ a.

No built-in arithmetic shortcuts — the comparison recurses through the `{L|R}` structure. Performance comes from per-instance evaluation caching and memoized operations, not from bypassing the surreal framework.

### Infinite sets

For numbers requiring infinite left/right options (ω, 1/ω, 1/3, √2, π, etc.), the library uses the `IInfiniteSet` interface. Each implementation answers comparison queries symbolically:

- **NaturalNumbers** `{0,1,2,...}` — "any element ≥ x?" → true if x is finite
- **DyadicApproxBelow/Above** — lazy binary search generators for rationals and irrationals
- **OmegaMinusNaturals** `{ω,ω-1,...}` — for ω/2's right options
- **OmegaMultiples** `{0,ω,2ω,...}` — for ω²'s left options
- **OmegaPowers** `{1,ω,ω²,...}` — for ω^ω's left options

Generators use a pluggable predicate ("is this dyadic below my target?") enabling any computable real number:

| Number | Predicate | Identity |
|--------|-----------|----------|
| p/q | `p·2^exp > mid·q` | Rational comparison |
| √n | `mid² < n·4^exp` | Square root |
| π | `mid < π·2^exp` | Via `Math.PI` |
| Custom | Any `Func<long,int,bool>` | Arbitrary Dedekind cut |

### Arithmetic

| Operation | Finite numbers | Non-finite numbers |
|-----------|---------------|-------------------|
| `+`, `-` | Conway formula + auto-simplify + memoization | TransfiniteAdd with sampled cross-terms |
| `*` | Conway formula + auto-simplify + memoization | Algebraic tag dispatch (√n·√m=√(nm), ω·ω=ω²) + FOIL expansion for symbolic sums |
| Negation | Swap and negate L/R | Propagates symbolic terms |
| Simplify | Evaluate to dyadic rational, reconstruct canonical form | Identity (no-op) |

## Project structure

```
Surreal/
├── Surr.cs              Core class: fields, constructors, evaluation, ToString
├── Surr.Operators.cs    Comparison and arithmetic operators
├── Surr.Factory.cs      Static factories: Dyadic, FromRational, FromSqrt, Pi, constants
├── Dyad.cs              Internal dyadic rational struct for evaluation/caching
├── Interfaces.cs        IInfiniteSet and all infinite set implementations
└── Starter.cs           Interactive demo

Surreal.Tests/
├── IntegerTests.cs      Addition, subtraction, multiplication, commutativity, associativity
├── ComparisonTests.cs   Equality, ordering, reflexive, transitive
├── DyadicTests.cs       Fractions, simplification, ToString
├── RationalTests.cs     1/3, 2/3, 1/5, 3/7, addition of rationals
├── SqrtTests.cs         √2, √3, √5, products, tight bounds
├── PiTests.cs           π bounds, comparisons
├── TransfiniteTests.cs  ω, ω±n, ω/2, √ω, 1/ω, difference of squares identity
├── OmegaPowerTests.cs   ω², ω^ω, n·ω, full ordering chain
├── PropertyTests.cs     Algebraic properties: distributivity, inverses, associativity
├── InfiniteSetEqualityTests.cs   {2,4,6,...|}=ω, {0|1/primes}=1/ω
└── CustomInfiniteSets.cs         Test-specific IInfiniteSet implementations
```

## Design philosophy

- **No cheating**: Runtime comparisons use Conway's recursive surreal definition, never C# arithmetic shortcuts. Integer arithmetic appears only during construction (defining what a set contains).
- **Lazy generation**: Infinite sets generate dyadic approximations on demand, caching results. No precision limits — termination is guaranteed by same-rule identity checks or interleaved bracket separation.
- **Symbolic tracking**: Surreals from transfinite addition carry symbolic expression terms, enabling FOIL expansion in multiplication (e.g., (√ω-√2)(√ω+√2) = ω-2).

## Requirements

- .NET 10 SDK

## References

- J.H. Conway, *On Numbers and Games* (1976)
- D.E. Knuth, *Surreal Numbers* (1974)
- E.R. Berlekamp, J.H. Conway, R.K. Guy, *Winning Ways for Your Mathematical Plays* (1982)
