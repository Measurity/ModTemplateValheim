using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ExampleCallMethodMod.Utils;

/// <summary>
///     Example command parser. Can be removed.
/// </summary>
public static class CommandParser
{
    private static readonly Func<string, int, TakerResult>[] segmentTakers =
    {
        TakeInt,
        TakeFloat,
        TakeString,
        TakeIdentifier
    };

    public enum SegmentType
    {
        Unknown,
        Int,
        Float,
        String,
        Identifier
    }

    public static IEnumerable<CommandSegment> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            yield break;
        }
        var index = 0;
        TakerResult takerResult;
        while ((takerResult = TakeSegment(input, index)) != null)
        {
            yield return takerResult.Segment;
            index = takerResult.Offset;
        }
    }

    private static TakerResult TakeSegment(string input, int offset, SegmentType type = SegmentType.Unknown)
    {
        while (true)
        {
            if (offset >= input.Length)
            {
                return null;
            }
            if (input[offset] != ' ')
            {
                break;
            }
            offset++;
        }

        var start = offset;
        TakerResult takerResult = null;
        foreach (var taker in segmentTakers)
        {
            takerResult = taker(input, offset);
            if (takerResult?.Segment != null)
            {
                break;
            }
        }
        return takerResult ?? new TakerResult(new CommandSegment(SegmentType.Unknown, input.Substring(start)));
    }

    private static TakerResult TakeIdentifier(string input, int offset)
    {
        Match match = Regex.Match(input.Substring(offset), @"^\w+");
        CommandSegment segment = match.Success ? new CommandSegment(SegmentType.Identifier, match.Value) : null;
        return new TakerResult(segment, offset + segment?.Text.Length ?? offset);
    }

    private static TakerResult TakeInt(string input, int offset)
    {
        Match match = Regex.Match(input.Substring(offset), @"^\-?\d+");
        CommandSegment segment = match.Success ? new CommandSegment(SegmentType.Int, match.Value) : null;
        return new TakerResult(segment, offset + segment?.Text.Length ?? offset);
    }

    private static TakerResult TakeFloat(string input, int offset)
    {
        Match match = Regex.Match(input.Substring(offset), @"^\-?\d+(?:\.\d+)?");
        CommandSegment segment = match.Success ? new CommandSegment(SegmentType.Float, match.Value) : null;
        return new TakerResult(segment, offset + segment?.Text.Length ?? offset);
    }

    private static TakerResult TakeString(string input, int offset)
    {
        if (input[offset] != '"')
        {
            return null;
        }

        StringBuilder sb = new();
        offset++;
        while (offset < input.Length)
        {
            if (input[offset] == '\\' && offset + 1 < input.Length)
            {
                // TODO: Special handling when '\t', '\n' or other.
                sb.Append(input[offset + 1]);
                offset += 2;
                continue;
            }
            if (input[offset] == '"')
            {
                offset++;
                break;
            }

            sb.Append(input[offset]);
            offset++;
        }
        return new TakerResult(new CommandSegment(SegmentType.String, sb.ToString()), offset);
    }

    public class CommandSegment
    {
        public CommandSegment(SegmentType type, string text)
        {
            Type = type;
            Text = text ?? "";
        }

        public SegmentType Type { get; }
        public string Text { get; }

        public override string ToString()
        {
            return Text;
        }
    }

    public class TakerResult
    {
        public TakerResult(CommandSegment segment)
        {
            Segment = segment;
            Offset = segment?.Text.Length ?? 0;
        }

        public TakerResult(CommandSegment segment, int offset) : this(segment)
        {
            Segment = segment;
            Offset = offset;
        }

        public CommandSegment Segment { get; }
        public int Offset { get; }
    }
}