using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Extensions;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// LRC Lyric Parser.
/// </summary>
public class LrcLyricParser : ILyricParser
{
    private readonly LyricParser _lrcLyricParser;

    private static readonly string[] _supportedMediaTypes = [".lrc", ".elrc"];

    /// <summary>
    /// Initializes a new instance of the <see cref="LrcLyricParser"/> class.
    /// </summary>
    public LrcLyricParser()
    {
        _lrcLyricParser = new LrcParser.Parser.Lrc.LrcParser();
    }

    /// <inheritdoc />
    public string Name => "LrcLyricProvider";

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public ResolverPriority Priority => ResolverPriority.Fourth;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Song lyricData;

        try
        {
            lyricData = _lrcLyricParser.Decode(lyrics.Content);
        }
        catch (Exception)
        {
            // Failed to parse, return null so the next parser will be tried
            return null;
        }

        List<LrcParser.Model.Lyric> sortedLyricData = lyricData.Lyrics.OrderBy(x => x.StartTime).ToList();

        if (sortedLyricData.Count == 0)
        {
            return null;
        }

        List<LyricLine> lyricList = [];

        for (int i = 0; i < sortedLyricData.Count; i++)
        {
            List<LyricLineTimeTag>? timeTags = null;
            if (sortedLyricData[i].TimeTags.Count != 0)
            {
                timeTags = sortedLyricData[i].TimeTags
                    .Where(kvp => kvp.Value.HasValue)
                    .Select(kvp => new LyricLineTimeTag(
                        position: Math.Max(kvp.Key.Index, 0),
                        start: TimeSpan.FromMilliseconds(kvp.Value ?? 0).Ticks))
                    .ToList();
            }

            long ticks = TimeSpan.FromMilliseconds(sortedLyricData[i].StartTime).Ticks;
            lyricList.Add(new LyricLine(sortedLyricData[i].Text.Trim(), ticks, timeTags));
        }

        return new LyricDto { Lyrics = lyricList };
    }
}
