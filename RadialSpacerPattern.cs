using System;
using System.Collections.Generic;
using System.Linq;

namespace TfmrLib
{
    public enum SpacerPatternExhaustedBehavior
    {
        Throw,
        RepeatLast,
        Cycle
    }

    public readonly record struct SpacerStep(
        int Index,
        double RawHeight_mm,
        double CompressedHeight_mm,
        SpacerPatternElement SourceElement,
        SpacerPatternElement? SubElement,
        bool FromComposite);

    public class RadialSpacerPattern
    {
        public double SpacerWidth_mm { get; set; }
        public int NumSpacers_Circumference { get; set; }
        public double AxialCompressionFactor { get; set; } = 0.954;
        public List<SpacerPatternElement> Elements { get; set; } = new();
        public string Description => string.Join(" + ", Elements.Select(e => e.ToString()));

        public double Height_mm =>
            Elements.Sum(e =>
            {
                if (e is CompositeSpacerPatternElement comp)
                {
                    double subSum = comp.SubElements.Sum(se => se.Count * se.SpacerHeight_mm);
                    return comp.Count * subSum;
                }
                return e.Count * e.SpacerHeight_mm;
            }) * AxialCompressionFactor;

        // Stream all individual spacer gaps (each repetition counted).
        public IEnumerable<SpacerStep> EnumerateSteps()
        {
            int idx = 0;
            foreach (var element in Elements)
            {
                if (element is CompositeSpacerPatternElement comp)
                {
                    for (int rep = 0; rep < comp.Count; rep++)
                    {
                        foreach (var sub in comp.SubElements)
                        {
                            for (int c = 0; c < sub.Count; c++)
                            {
                                double raw = sub.SpacerHeight_mm;
                                yield return new SpacerStep(idx++, 
                                                            raw, 
                                                            raw * AxialCompressionFactor,
                                                            comp,
                                                            sub,
                                                            true);
                            }
                        }
                    }
                }
                else
                {
                    for (int c = 0; c < element.Count; c++)
                    {
                        double raw = element.SpacerHeight_mm;
                        yield return new SpacerStep(
                            idx++,
                            raw,
                            raw * AxialCompressionFactor,
                            element,
                            null,
                            false);
                    }
                }
            }
        }

        private List<double>? _flattenedCompressed;
        public IReadOnlyList<double> FlattenedHeightsCompressed =>
            _flattenedCompressed ??= EnumerateSteps()
                .Select(s => s.CompressedHeight_mm)
                .ToList();

        // Get enumerator that yields exactly requiredCount spacer heights with chosen exhaustion behavior.
        public IEnumerator<double> GetGapEnumerator(int requiredCount, SpacerPatternExhaustedBehavior behavior = SpacerPatternExhaustedBehavior.Throw)
        {
            return GapIterator(requiredCount, behavior).GetEnumerator();
        }

        private IEnumerable<double> GapIterator(int requiredCount, SpacerPatternExhaustedBehavior behavior)
        {
            var list = FlattenedHeightsCompressed;
            if (list.Count == 0)
            {
                if (requiredCount > 0) throw new InvalidOperationException("Spacer pattern has no elements.");
                yield break;
            }

            int i = 0;
            for (; i < requiredCount; i++)
            {
                if (i < list.Count)
                {
                    yield return list[i];
                }
                else
                {
                    switch (behavior)
                    {
                        case SpacerPatternExhaustedBehavior.Throw:
                            throw new InvalidOperationException($"Spacer pattern exhausted after {list.Count} gaps; need {requiredCount}.");
                        case SpacerPatternExhaustedBehavior.RepeatLast:
                            yield return list[^1];
                            break;
                        case SpacerPatternExhaustedBehavior.Cycle:
                            yield return list[i % list.Count];
                            break;
                    }
                }
            }
        }
    }

    public class SpacerPatternElement
    {
        public int Count { get; set; }
        public double SpacerHeight_mm { get; set; }
        public override string ToString() => $"{Count} × {SpacerHeight_mm} mm";
    }

    public class CompositeSpacerPatternElement : SpacerPatternElement
    {
        public List<SpacerPatternElement> SubElements { get; set; } = new();
        public override string ToString() =>
            $"{Count} × ({string.Join(" + ", SubElements.Select(e => e.ToString()))})";
    }
}
