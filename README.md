# Surreal

A C# implementation of [Conway's surreal numbers](https://en.wikipedia.org/wiki/Surreal_number) — a number system that unifies integers, fractions, irrationals, infinitesimals, and transfinite ordinals under a single recursive definition.

## The number line

This library can represent and compare numbers across the entire surreal number line:

```
-Γ₀ < -ω < -100 < 0 < 1/Γ₀ < 1/√Γ₀ < 1/ε₀ < 1/ω < 1/3 < √2 < e < π < 100
  < log(ω) < √ω < ω/2 < ω < 2ω < ω² < ω^ω < ε₀ < ε₁ < ζ₀ < Γ₀
```

From negative transfinite ordinals through infinitesimals, reals (rational, irrational, transcendental), to the Feferman-Schütte ordinal. Also supports combinatorial games (*, ↑, ↓, nimbers).

## Quick start

```bash
dotnet build
dotnet run           # runs Starter.cs demo
dotnet test          # runs 312 tests
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

// Division
Console.WriteLine(new Surr(1) / new Surr(3));         // 1/3
Console.WriteLine(Surr.FromSqrt(8) / Surr.FromSqrt(2)); // 2 (= √4)
Console.WriteLine(Surr.Omega / 3);                     // ω/3

// Combinatorial games (non-numeric surreals)
var star = Surr.Star;                 // * = {0|0}
Console.WriteLine(star.IsNumeric);    // False
Console.WriteLine(star <= 0);         // False (fuzzy with 0!)
Console.WriteLine(star >= 0);         // False
Console.WriteLine(star + star == 0);  // True (* + * = 0)
Console.WriteLine(Surr.Up > 0);      // True (↑ is positive infinitesimal game)
Console.WriteLine(Surr.Down < 0);    // True

// Nimbers (Sprague-Grundy values)
var n2 = Surr.Nimber(2);             // *2 = {0,*|0,*}
Console.WriteLine(n2 != star);       // True (*2 ≠ *)
Console.WriteLine(n2 + n2 == 0);     // True (*n + *n = 0)

// Algebraic identities work across number types
var sw = Surr.SqrtOmega;
var s2 = Surr.FromSqrt(2);
Console.WriteLine((sw - s2) * (sw + s2) + new Surr(2) == w);  // True
// (√ω - √2)(√ω + √2) + 2 = ω  ✓

// Custom Dedekind cuts
var phi = Surr.FromPredicate(
    (mid, exp) => mid * mid < (mid + (1L << exp)) * (1L << exp),
    1, "φ");  // golden ratio via mid² < mid + 1

// Large ordinals and the Veblen hierarchy
Console.WriteLine(Surr.EpsilonNaught > Surr.OmegaToOmega); // True (ε₀ > ω^ω)
Console.WriteLine(Surr.Pow(Surr.Omega, Surr.EpsilonNaught)
    == Surr.EpsilonNaught);                                   // True (ω^ε₀ = ε₀)
Console.WriteLine(Surr.Zeta0 > Surr.Epsilon(4));             // True (ζ₀ > all ε_n)
Console.WriteLine(Surr.Gamma0 > Surr.Zeta0);                 // True (Γ₀ > ζ₀)

// Infinitesimals smaller than 1/ω
Console.WriteLine(Surr.InverseGamma0 > 0);           // True (1/Γ₀ is positive)
Console.WriteLine(Surr.InverseGamma0 < Surr.InverseOmega); // True

// Logarithms
Console.WriteLine(Surr.LogOmega > 100000);             // True (log(ω) > all finite)
Console.WriteLine(Surr.LogGamma0 > Surr.LogOmega);     // True

// Birthday and sign expansion
Console.WriteLine(Surr.Birthday(Surr.Half));            // 2 (born day 2)
Console.WriteLine(Surr.SignExpansion(Surr.Dyadic(3,2))); // "+-+" (path to 3/4)

// Temperature (game thermography)
var hot = new Surr(new[] { new Surr(5) }, new[] { new Surr(-3) });
Console.WriteLine(Surr.Temperature(hot));               // 4
Console.WriteLine(Surr.Mean(hot));                       // 1

// Nim multiplication
Console.WriteLine(Surr.NimProduct(2, 3));               // 1 (*2 ⊗ *3 = *1)
```

## How it works

Every surreal number is a pair `{L | R}` where L (left options) and R (right options) are sets of previously-created surreal numbers, with every element of L less than every element of R. The value is the "simplest" number that fits between L and R.

### Comparison

All comparisons use Conway's recursive definition:

> **a ≤ b** iff no left option of a is ≥ b, and no right option of b is ≤ a.

No built-in arithmetic shortcuts — the comparison recurses through the `{L|R}` structure. Performance comes from memoized `<=` results (cached by reference identity), per-instance evaluation caching, and memoized arithmetic operations.

### Infinite sets

For numbers requiring infinite left/right options (ω, 1/ω, 1/3, √2, π, etc.), the library uses the `IInfiniteSet` interface. Each implementation answers comparison queries symbolically:

- **NaturalNumbers** `{0,1,2,...}` — for ω's left options
- **NegatedSet** — wraps any set, negating all elements (for `-ω`, `-Γ₀`, etc.)
- **DyadicApproxBelow/Above** — lazy binary search generators for rationals/irrationals
- **OmegaMinusNaturals** `{ω,ω-1,...}` — for ω/2's right options
- **OmegaMultiples** `{0,ω,2ω,...}` — for ω²'s left options
- **OmegaPowers** `{1,ω,ω²,...}` — for ω^ω's left options
- **TransfinitePlusNaturals** `{base+0,base+1,...}` — for n·ω's left options
- **ShiftedNaturals** `{k+0,k+1,...}` — for transfinite + finite sums
- **PositivePowersOfHalf** `{1,1/2,1/4,...}` — for 1/ω's right options
- **OmegaPowersOfHalf** `{ω,ω/2,ω/4,...}` — for √ω's right options

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
| `+`, `-` | Conway formula + auto-simplify + memoization | TransfiniteAdd with sampled cross-terms + symbolic sum decomposition. Full Conway formula for finite games. |
| `*` | Conway formula + auto-simplify + memoization | Algebraic tag dispatch (√n·√m=√(nm), ω·ω=ω²) + FOIL expansion with automatic cancellation of opposite terms |
| `/` | Algebraic: dyadic/dyadic, rational/rational, √n/√m, ω/n | General Conway inverse not yet implemented |
| `Pow` | Integer exponentiation | ω^n, ω^ω, ω^ε₀=ε₀, n^ω=ω |
| Negation | Swap and negate L/R | NegatedSet wrapping + symbolic term propagation |
| Simplify | Evaluate to dyadic rational, reconstruct canonical form | Identity (no-op) |

## Project structure

```
Surreal/
├── Surr.cs              Core class: fields, constructors, evaluation, ToString
├── Surr.Operators.cs    Comparison (+, -, *, /, Pow, <=) and arithmetic operators
├── Surr.Factory.cs      Static factories, constants, Birthday, SignExpansion, Temperature
├── Dyad.cs              Internal dyadic rational struct for evaluation/caching
├── Interfaces.cs        IInfiniteSet implementations (15+ types)
├── OmegaPolynomial.cs   ω-polynomial representation (c_n·ω^n + ... + c_0)
└── Starter.cs           Interactive demo

Surreal.Tests/
├── IntegerTests.cs      +, -, *, commutativity, associativity, identity
├── ComparisonTests.cs   Equality, ordering, reflexive, transitive
├── DyadicTests.cs       Fractions, simplification, ToString
├── RationalTests.cs     1/3, 2/3, 1/5, 3/7, rational arithmetic
├── SqrtTests.cs         √2, √3, √5, products, (√2)⁴=4
├── PiTests.cs           π bounds and comparisons
├── TransfiniteTests.cs  ω, ω±n, ω/2, √ω, 1/ω, FOIL identities
├── OmegaPowerTests.cs   ω², ω^ω, n·ω, full ordering chain
├── LargeOrdinalTests.cs ε₀, ε₁, ζ₀, Γ₀, Veblen hierarchy, fixed points
├── PropertyTests.cs     Distributivity, inverses, associativity
├── DivisionTests.cs     Integer, rational, sqrt, transfinite division
├── GameTests.cs         *, ↑, ↓, nimbers, game addition, fuzzy comparisons
├── NimTests.cs          Nim product ⊗, 4×4 table, associativity, distributivity
├── ThermographyTests.cs Temperature and mean of games
├── OmegaPolyTests.cs   ω-polynomials: ω²+3ω+5, ordering
├── DeepTests.cs         1/Γ₀, log(ω), negation, mixed number line
├── ConstantsAndStructureTests.cs  e, ε₀, birthday, sign expansion, Pow
├── InfiniteSetEqualityTests.cs    {evens}=ω, {inverse primes}=1/ω
└── CustomInfiniteSets.cs          Test-specific IInfiniteSet implementations
```

## Design philosophy

- **No cheating**: Runtime comparisons use Conway's recursive surreal definition, never C# arithmetic shortcuts. Integer arithmetic appears only during construction (defining what a set contains).
- **Memoized comparison**: The `<=` operator caches results by reference identity, enabling efficient deep comparison of game trees and complex surreal structures.
- **Lazy generation**: Infinite sets generate dyadic approximations on demand, caching results. No precision limits — termination is guaranteed by same-rule identity checks or interleaved bracket separation.
- **Symbolic tracking**: Surreals from transfinite addition carry symbolic expression terms, enabling FOIL expansion in multiplication (e.g., (√ω-5)(√ω+5) = ω-25).
- **Generalized negation**: `NegatedSet` wraps any `IInfiniteSet`, automatically negating all elements via redirected comparison queries. Double negation unwraps.

## Requirements

- .NET 10 SDK

## References

- J.H. Conway, *On Numbers and Games* (1976)
- D.E. Knuth, *Surreal Numbers* (1974)
- E.R. Berlekamp, J.H. Conway, R.K. Guy, *Winning Ways for Your Mathematical Plays* (1982)
- Wikipedia: [Surreal number](https://en.wikipedia.org/wiki/Surreal_number), [Veblen hierarchy](https://en.wikipedia.org/wiki/Veblen_function), [Nimber](https://en.wikipedia.org/wiki/Nimber)
