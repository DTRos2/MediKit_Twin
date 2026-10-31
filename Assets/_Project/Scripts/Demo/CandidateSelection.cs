using System;
using System.Collections.Generic;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 후보 목록에서 하나를 고르고, 돌려 가며 바꾼다. 특정 후보만 쓰겠다고 지정하면 그것만 후보로 남긴다.
    /// 지정한 후보가 목록에 없으면 전체를 후보로 두고 IsOnlyMissing으로 알린다(설정 실수를 숨기지 않기 위해서다).
    /// 후보가 어떤 종류인지 모르므로 로봇 말고 다른 장치를 고르는 패널에도 쓴다. 화면을 그리는 코드와 분리해 씬 없이 시험한다.
    /// </summary>
    public sealed class CandidateSelection<T> where T : class
    {
        private readonly List<T> _candidates;
        private int _index;

        /// <param name="candidates">고를 수 있는 후보 전체.</param>
        /// <param name="only">null이 아니면 이 후보만 쓴다. 후보 목록에 없으면 무시하고 IsOnlyMissing을 켠다.</param>
        public CandidateSelection(IEnumerable<T> candidates, T only = null)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            _candidates = new List<T>(candidates);
            if (only == null)
            {
                return;
            }

            if (_candidates.Contains(only))
            {
                _candidates.Clear();
                _candidates.Add(only);
            }
            else
            {
                IsOnlyMissing = true;
            }
        }

        public int Count => _candidates.Count;

        public bool IsEmpty => _candidates.Count == 0;

        /// <summary>바꿀 수 있는 후보가 둘 이상이면 true.</summary>
        public bool HasChoice => _candidates.Count > 1;

        /// <summary>하나만 쓰겠다고 지정했지만 그 후보가 목록에 없었으면 true.</summary>
        public bool IsOnlyMissing { get; }

        /// <summary>지금 고른 후보. 후보가 없으면 예외다. IsEmpty를 먼저 확인한다.</summary>
        public T Current
        {
            get
            {
                ThrowIfEmpty();
                return _candidates[_index];
            }
        }

        /// <summary>다음 후보로 넘어가고(끝이면 처음으로) 그 후보를 돌려준다.</summary>
        public T SelectNext()
        {
            ThrowIfEmpty();
            _index = (_index + 1) % _candidates.Count;
            return _candidates[_index];
        }

        private void ThrowIfEmpty()
        {
            if (_candidates.Count == 0)
            {
                throw new InvalidOperationException("고를 수 있는 후보가 없다.");
            }
        }
    }
}
