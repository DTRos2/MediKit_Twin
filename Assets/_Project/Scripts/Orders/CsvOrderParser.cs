using System;
using System.Collections.Generic;
using System.Globalization;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// CSV 줄을 주문으로 바꾸는 흐름만 맡는다. (FR-01) 글자 나누기는 CsvLineSplitter, 열 위치는 CsvOrderColumns,
    /// 값 검사는 IOrderRowValidator가 맡으므로 이 클래스는 규칙이 늘어도 바뀌지 않는다.
    /// 잘못된 행은 전체를 멈추지 않고 건너뛰며 경고로 남긴다. 첫 줄의 순번이 숫자가 아니면 머리글로 보고 건너뛴다.
    /// 병원명은 비어 있어도 받는다. 라벨이 "UNKNOWN"으로 표시하고 경고를 남기는 일은 FR-11의 몫이다.
    /// </summary>
    public sealed class CsvOrderParser : IOrderParser
    {
        private const char CommentMarker = '#';

        private readonly CsvLineSplitter _splitter;
        private readonly CsvOrderColumns _columns;
        private readonly IReadOnlyList<IOrderRowValidator> _validators;

        public CsvOrderParser(CsvLineSplitter splitter, CsvOrderColumns columns, IReadOnlyList<IOrderRowValidator> validators)
        {
            _splitter = splitter ?? throw new ArgumentNullException(nameof(splitter));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        }

        public OrderLoadResult Parse(string text)
        {
            var orders = new List<OrderRecord>();
            var issues = new List<OrderIssue>();
            var context = new OrderValidationContext();

            string[] lines = (text ?? string.Empty).TrimStart('﻿').Split('\n');
            bool isFirstContentLine = true;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == CommentMarker)
                {
                    continue;
                }

                int lineNumber = i + 1;
                bool isHeaderCandidate = isFirstContentLine;
                isFirstContentLine = false;

                List<string> fields = _splitter.Split(line);
                if (!TryMapRow(fields, isHeaderCandidate, out OrderRecord order, out string reason))
                {
                    if (reason != null)
                    {
                        issues.Add(new OrderIssue(OrderIssueCodes.InvalidRow, $"{lineNumber}행을 건너뛴다: {reason}"));
                    }

                    continue;
                }

                if (TryValidate(order, lineNumber, context, out OrderIssue issue))
                {
                    orders.Add(order);
                }
                else
                {
                    issues.Add(issue);
                }
            }

            return new OrderLoadResult(orders, issues);
        }

        // 머리글로 보이는 첫 줄은 reason 없이 거부해 경고를 남기지 않는다.
        private bool TryMapRow(List<string> fields, bool isHeaderCandidate, out OrderRecord order, out string reason)
        {
            order = null;
            reason = null;

            if (_columns.SequenceIndex >= fields.Count
                || !int.TryParse(fields[_columns.SequenceIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sequence))
            {
                reason = isHeaderCandidate ? null : "순번이 정수가 아니다";
                return false;
            }

            if (fields.Count != _columns.RequiredCount)
            {
                reason = $"열이 {_columns.RequiredCount}개여야 한다(현재 {fields.Count}개)";
                return false;
            }

            string quantityText = fields[_columns.QuantityIndex];
            if (!int.TryParse(quantityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity))
            {
                reason = $"수량 '{quantityText}'이 정수가 아니다";
                return false;
            }

            order = new OrderRecord(sequence, fields[_columns.HospitalNameIndex], fields[_columns.KitCodeIndex], quantity);
            return true;
        }

        private bool TryValidate(OrderRecord order, int lineNumber, OrderValidationContext context, out OrderIssue issue)
        {
            for (int i = 0; i < _validators.Count; i++)
            {
                if (!_validators[i].TryValidate(order, lineNumber, context, out issue))
                {
                    return false;
                }
            }

            issue = default;
            return true;
        }
    }
}
