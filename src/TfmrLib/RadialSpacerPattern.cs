using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TfmrLib
{
    public class RadialSpacerPattern : IEnumerable<SpacerPatternElement>
    {
        public double SpacerWidth_mm { get; set; }
        public int NumSpacers_Circumference { get; set; }
        public double AxialCompressionFactor { get; set; } = 0.954;
        public List<SpacerPatternElement> Elements { get; set; } = new();

        public int Count => Elements.Sum(elements => elements.Count);

        public double Height_mm => this.Sum<SpacerPatternElement>(element => element.SpacerHeight_mm);

        public IEnumerator<SpacerPatternElement> GetEnumerator() => new SpacerEnumerator(Elements);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Custom Enumerator implementation
        private class SpacerEnumerator : IEnumerator<SpacerPatternElement>
        {
            private List<SpacerPatternElement> _elements;
            private Stack<SpacerPatternElement> _stack;

            private SpacerPatternElement _current = null;

            public SpacerEnumerator(List<SpacerPatternElement> elements)
            {
                _elements = elements;
                Reset();
            }

            public SpacerPatternElement Current
            {
                get
                {
                    if (_current is null)
                        throw new InvalidOperationException();
                    return _current;
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_stack.Count > 0)
                {
                    _current = _stack.Pop();
                    if (_current is CompositeSpacerPatternElement comp)
                    {
                        for (int i = 0; i < comp.RepetitionCount; i++)
                        {
                            foreach (SpacerPatternElement element in comp.SubElements)
                            {
                                _stack.Push(element);
                            }
                        }
                        return MoveNext();
                    }
                    return true;
                }
                _current = null;
                return false;
            }

            public void Reset() => new Stack<SpacerPatternElement>(_elements.AsEnumerable().Reverse());

            public void Dispose() { /* Clean up unmanaged resources if any */ }
        }
    }

    public class SpacerPatternElement
    {
        public int Count { get; set; } = 1;
        public double SpacerHeight_mm { get; set; }
    }

    public class CompositeSpacerPatternElement : SpacerPatternElement
    {
        public List<SpacerPatternElement> SubElements { get; set; } = new();

        public int RepetitionCount { get; set; } = 1;

        public int Count => RepetitionCount * SubElements.Sum(subelements => subelements.Count);
    }


}
